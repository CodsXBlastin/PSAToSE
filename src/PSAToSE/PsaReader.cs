using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSAToSE
{
    internal class PsaHeader
    {
        public string ChunkID { get; set; }
        public int TypeFlag { get; set; }
        public int DataSize { get; set; }
        public int DataCount { get; set; }
    }

    internal class PsaBone
    {
        public string Name { get; set; }
        public int Flags { get; set; }
        public int Children { get; set; }
        public int ParentIndex { get; set; }
        public float RotationX { get; set; }
        public float RotationY { get; set; }
        public float RotationZ { get; set; }
        public float RotationW { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
    }

    internal class PsaAnimChunk
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public int BoneCount { get; set; }
        public int RootInclude { get; set; }
        public int CompressionFlags { get; set; }
        public int KeyQuotum { get; set; }
        public float KeyReduction { get; set; }
        public float TrackTime { get; set; }
        public float AnimationRate { get; set; }
        public int StartBone { get; set; }
        public int FirstRawFrame { get; set; }
        public int RawFrameCount { get; set; }
    }

    internal class PsaAnimKey
    {
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public float RotationX { get; set; }
        public float RotationY { get; set; }
        public float RotationZ { get; set; }
        public float RotationW { get; set; }
        public float Time { get; set; }
    }

    internal class PsaReader
    {
        public static void SerializePSA(string Input)
        {
            using (var Reader = new BinaryReader(File.OpenRead(Input)))
            {
                // Buffers for data
                List<PsaBone> Bones = null;
                List<PsaAnimChunk> AnimChunks = null;
                List<PsaAnimKey> AnimKeys = null;

                while (Reader.BaseStream.Position != Reader.BaseStream.Length)
                {
                    // Read header
                    var CurrentHeader = ReadHeader(Reader);

                    // Read the blocks
                    switch (CurrentHeader.ChunkID)
                    {
                        case "BONENAMES": Bones = ReadBoneBlock(Reader, CurrentHeader); break;
                        case "ANIMINFO": AnimChunks = ReadAnimBlock(Reader, CurrentHeader); break;
                        case "ANIMKEYS": AnimKeys = ReadKeyBlock(Reader, CurrentHeader); break;
                    }
                }

                // Ensure we have data
                if (Bones.Count > 0 && AnimChunks.Count > 0 && AnimKeys.Count > 0)
                {
                    // We got it! Prepare to generate keys
                    var OutputDirectory = Path.Combine(Path.GetDirectoryName(Input), "exported_files");
                    Directory.CreateDirectory(OutputDirectory);

                    // Save our offset for later
                    var CurrentKeyOffset = 0;

                    // Iterate over the chunks, each chunk is an anim file
                    foreach (var Chunk in AnimChunks)
                    {
                        var Anim = new SELib.SEAnim();

                        // Count the keyframes we need
                        for (int f = 0; f < Chunk.RawFrameCount; f++)
                        {
                            // Iterate over bones
                            for (int b = Chunk.StartBone; b < Chunk.BoneCount; b++)
                            {
                                // Grab the frame and add it
                                var Frame = AnimKeys[CurrentKeyOffset++];
                                var Bone = Bones[b];

                                // Add it
                                Anim.AddTranslationKey(Bone.Name, f, Frame.PositionX, Frame.PositionY, Frame.PositionZ);

                                // Check for retarded quaternion inversion
                                if (Frame.RotationW < 0.0)
                                {
                                    Anim.AddRotationKey(Bone.Name, f, -Frame.RotationX, -Frame.RotationY, -Frame.RotationZ, Frame.RotationW);
                                }
                                else
                                {
                                    Anim.AddRotationKey(Bone.Name, f, Frame.RotationX, Frame.RotationY, Frame.RotationZ, Frame.RotationW);
                                }
                            }
                        }

                        // Save to a file, log it
                        Anim.Write(Path.Combine(OutputDirectory, Chunk.Name + ".seanim"));
                        Console.WriteLine("Wrote: {0}", Path.Combine(OutputDirectory, Chunk.Name + ".seanim"));
                    }

#if DEBUG
                    if (CurrentKeyOffset != AnimKeys.Count)
                        System.Diagnostics.Debugger.Break();
#endif
                }
                else
                {
                    Console.WriteLine("Input file contained no valid animation data!");
                }
            }
        }

        private static List<PsaAnimKey> ReadKeyBlock(BinaryReader Reader, PsaHeader Header)
        {
            var Result = new List<PsaAnimKey>();

            for (int i = 0; i < Header.DataCount; i++)
            {
                using (var DataReader = new BinaryReader(new MemoryStream(Reader.ReadBytes(Header.DataSize))))
                {
                    var Key = new PsaAnimKey();

                    Key.PositionX = DataReader.ReadSingle();
                    Key.PositionY = DataReader.ReadSingle();
                    Key.PositionZ = DataReader.ReadSingle();
                    Key.RotationX = DataReader.ReadSingle();
                    Key.RotationY = DataReader.ReadSingle();
                    Key.RotationZ = DataReader.ReadSingle();
                    Key.RotationW = DataReader.ReadSingle();
                    Key.Time = DataReader.ReadSingle();

                    Result.Add(Key);
                }
            }

            return Result;
        }

        private static List<PsaAnimChunk> ReadAnimBlock(BinaryReader Reader, PsaHeader Header)
        {
            var Result = new List<PsaAnimChunk>();

            for (int i = 0; i < Header.DataCount; i++)
            {
                using (var DataReader = new BinaryReader(new MemoryStream(Reader.ReadBytes(Header.DataSize))))
                {
                    var Chunk = new PsaAnimChunk();

                    Chunk.Name = Encoding.ASCII.GetString(DataReader.ReadBytes(64)).Replace("\0", "");
                    Chunk.Group = Encoding.ASCII.GetString(DataReader.ReadBytes(64)).Replace("\0", "");
                    Chunk.BoneCount = DataReader.ReadInt32();
                    Chunk.RootInclude = DataReader.ReadInt32();
                    Chunk.CompressionFlags = DataReader.ReadInt32();
                    Chunk.KeyQuotum = DataReader.ReadInt32();
                    Chunk.KeyReduction = DataReader.ReadSingle();
                    Chunk.TrackTime = DataReader.ReadSingle();
                    Chunk.AnimationRate = DataReader.ReadSingle();
                    Chunk.StartBone = DataReader.ReadInt32();
                    Chunk.FirstRawFrame = DataReader.ReadInt32();
                    Chunk.RawFrameCount = DataReader.ReadInt32();

                    Result.Add(Chunk);
                }
            }

            return Result;
        }

        private static List<PsaBone> ReadBoneBlock(BinaryReader Reader, PsaHeader Header)
        {
            var Result = new List<PsaBone>();

            for (int i = 0; i < Header.DataCount; i++)
            {
                using (var DataReader = new BinaryReader(new MemoryStream(Reader.ReadBytes(Header.DataSize))))
                {
                    var Bone = new PsaBone();

                    Bone.Name = Encoding.ASCII.GetString(DataReader.ReadBytes(64)).Replace("\0", "");
                    Bone.Flags = DataReader.ReadInt32();
                    Bone.Children = DataReader.ReadInt32();
                    Bone.ParentIndex = DataReader.ReadInt32();
                    Bone.RotationX = DataReader.ReadSingle();
                    Bone.RotationY = DataReader.ReadSingle();
                    Bone.RotationZ = DataReader.ReadSingle();
                    Bone.RotationW = DataReader.ReadSingle();
                    Bone.PositionX = DataReader.ReadSingle();
                    Bone.PositionY = DataReader.ReadSingle();
                    Bone.PositionZ = DataReader.ReadSingle();

                    Result.Add(Bone);
                }
            }

            return Result;
        }

        private static PsaHeader ReadHeader(BinaryReader Reader)
        {
            var Result = new PsaHeader();

            Result.ChunkID = Encoding.ASCII.GetString(Reader.ReadBytes(0x14)).Replace("\0", "");
            Result.TypeFlag = Reader.ReadInt32();
            Result.DataSize = Reader.ReadInt32();
            Result.DataCount = Reader.ReadInt32();

            return Result;
        }
    }
}

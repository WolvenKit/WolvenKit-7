using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommandLine;
using CsvHelper;
using WolvenKit.Common.Extensions;
using WolvenKit.CR2W;
using WolvenKit.CR2W.JSON;
using WolvenKit.CR2W.SRT;
using WolvenKit.CR2W.Types;

namespace WolvenKit.CLI
{
    internal class LipsyncConverter
    {
        public static uint GetValueOrZeroUInt32(CUInt32 var)
        {
            return (var != null) ? var.val : (uint)0;
        }
        public static ushort GetValueOrZeroUInt16(CUInt16 var)
        {
            return (var != null) ? var.val : (ushort)0;
        }
        public static sbyte GetValueOrZeroInt8(CInt8 var)
        {
            return (var != null) ? var.val : (sbyte)0;
        }
        public static float GetValueOrZeroFloat(CFloat var)
        {
            return (var != null) ? var.val : (float)0.0;
        }
        public static bool LipsyncConvertCR2WToRaw(String inputPath, String outputPath)
        {
            var cr2w = CR2WJsonTool.ReadCR2W(inputPath);
            if (cr2w == null)
            {
                return false;
            }
            if (cr2w.chunks[0].REDType != "CSkeletalAnimation")
            {
                Program.PrintError($"CR2W Root chunk is {cr2w.chunks[0].REDType}, expected CSkeletalAnimation!");
                return false;
            }
            CSkeletalAnimation anim = cr2w.chunks[0].data as CSkeletalAnimation;
            if (anim.AnimBuffer == null || anim.AnimBuffer.Reference.data == null)
            {
                Program.PrintError($"CR2W AnimBuffer is null!");
                return false;
            }
            CAnimationBufferBitwiseCompressed animBuffer = anim.AnimBuffer.Reference.data as CR2W.Types.CAnimationBufferBitwiseCompressed;

            using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(fs))
            {
                writer.Write(anim.FramesPerSecond.val);
                writer.Write(anim.Duration.val);

                if (animBuffer.Bones != null)
                {
                    var bonesList = animBuffer.Bones.Elements;
                    writer.WriteVLQInt32(bonesList.Count);
                    for (int i = 0; i < bonesList.Count; i += 1)
                    {
                        var list = new List<SAnimationBufferBitwiseCompressedData> { bonesList[i].Position, bonesList[i].Orientation };
                        foreach (var compressedData in list)
                        {
                            writer.Write(GetValueOrZeroFloat(compressedData.Dt));
                            writer.Write(GetValueOrZeroInt8(compressedData.Compression));
                            writer.Write((sbyte)0);  // alignment!
                            writer.Write(GetValueOrZeroUInt16(compressedData.NumFrames));
                            writer.Write(GetValueOrZeroUInt32(compressedData.DataAddr));
                            writer.Write(GetValueOrZeroUInt32(compressedData.DataAddrFallback));
                        }
                    }
                }
                else
                {
                    writer.WriteVLQInt32(0);
                }

                if (animBuffer.Tracks != null)
                {
                    var tracksList = animBuffer.Tracks.Elements;
                    writer.WriteVLQInt32(tracksList.Count);
                    for (int i = 0; i < tracksList.Count; i += 1)
                    {
                        writer.Write(GetValueOrZeroFloat(tracksList[i].Dt));
                        writer.Write(GetValueOrZeroInt8(tracksList[i].Compression));
                        writer.Write((sbyte)0);  // alignment!
                        writer.Write(GetValueOrZeroUInt16(tracksList[i].NumFrames));
                        writer.Write(GetValueOrZeroUInt32(tracksList[i].DataAddr));
                        writer.Write(GetValueOrZeroUInt32(tracksList[i].DataAddrFallback));
                    }
                }
                else
                {
                    writer.WriteVLQInt32(0);
                }

                if (animBuffer.Data != null)
                {
                    var data = animBuffer.Data;
                    writer.WriteVLQInt32(data.Bytes.Length);
                    if (data.Bytes.Length > 0)
                        writer.Write(data.Bytes);
                }
                else
                {
                    writer.WriteVLQInt32(0);
                }
                writer.Write(GetValueOrZeroUInt32(animBuffer.NumFrames));
                writer.Write(GetValueOrZeroFloat(animBuffer.Dt));
            }
            return true;
        }
        public static bool LipsyncConvertRawToCR2W(String inputPath, String outputPath)
        {
            var cr2w = new CR2WFile();
            using (var fs = new FileStream(inputPath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fs))
            {
                var animChunk = cr2w.CreateChunk("CSkeletalAnimation", cr2w.chunks.Count);
                var bufferChunk = cr2w.CreateChunk("CAnimationBufferBitwiseCompressed", cr2w.chunks.Count);
                bufferChunk.ParentChunk = animChunk;
                bufferChunk.MountChunkVirtually(animChunk);

                var animData = (CSkeletalAnimation)animChunk.data;
                var bufferData = (CAnimationBufferBitwiseCompressed)bufferChunk.data;
                // init vars
                animData.GetEditableVariables();
                bufferData.GetEditableVariables();

                animData.BitwiseCompressionPreset.SetValue(new List<string> { "ABBCP_VeryHighQuality" });
                var settingsList = new List<SAnimationBufferBitwiseCompressionSettings>{ animData.BitwiseCompressionSettings, bufferData.CompressionSettings };
                foreach (var settings in settingsList)
                {
                    settings.GetEditableVariables();
                    settings.TranslationTolerance.SetValue(0.0);
                    settings.TranslationSkipFrameTolerance.SetValue(0.0);
                    settings.OrientationTolerance.SetValue(0.00005);
                    settings.OrientationCompressionMethod.SetValue(new List<string> { "ABOCM_PackIn64bitsW" });
                    settings.OrientationSkipFrameTolerance.SetValue(0.0001);
                    settings.ScaleTolerance.SetValue(0.001);
                    settings.ScaleSkipFrameTolerance.SetValue(0.001);
                    settings.TrackTolerance.SetValue(0.0);
                    settings.TrackSkipFrameTolerance.SetValue(0.0005);
                }
                animData.AnimBuffer.SetValue(bufferChunk);
                animData.FramesPerSecond.SetValue(reader.ReadSingle());
                animData.Duration.SetValue(reader.ReadSingle());

                bufferData.CompressionPreset.SetValue(new List<string> { "ABBCP_VeryHighQuality" });
                bufferData.Version.SetValue(2);
                bufferData.DeferredData.GetEditableVariables();
                bufferData.DeferredData.Bufferdata.SetValue(0);
                bufferData.NonStreamableBones.SetValue(2);

                var bonesCount = reader.ReadVLQInt32();
                for (int i = 0; i < bonesCount; i++)
                {
                    var bone = (SAnimationBufferBitwiseCompressedBoneTrack)CR2WTypeManager.Create("SAnimationBufferBitwiseCompressedBoneTrack", i.ToString(), cr2w, bufferData.Bones);
                    bone.GetEditableVariables();
                    var dataList = new List<SAnimationBufferBitwiseCompressedData> { bone.Position, bone.Orientation };
                    foreach (var compressedData in dataList)
                    {
                        compressedData.GetEditableVariables();
                        compressedData.Dt.SetValue(reader.ReadSingle());
                        compressedData.Compression.SetValue(reader.ReadSByte());
                        reader.ReadSByte();  // alignment!
                        compressedData.NumFrames.SetValue(reader.ReadUInt16());
                        compressedData.DataAddr.SetValue(reader.ReadUInt32());
                        compressedData.DataAddrFallback.SetValue(reader.ReadUInt32());
                    }
                    bufferData.Bones.Add(bone);
                }

                var tracksCount = reader.ReadVLQInt32();
                for (int i = 0; i < tracksCount; i++)
                {
                    var track = (SAnimationBufferBitwiseCompressedData)CR2WTypeManager.Create("SAnimationBufferBitwiseCompressedData", i.ToString(), cr2w, bufferData.Tracks);
                    track.GetEditableVariables();
                    track.Dt.SetValue(reader.ReadSingle());
                    track.Compression.SetValue(reader.ReadSByte());
                    reader.ReadSByte();  // alignment!
                    track.NumFrames.SetValue(reader.ReadUInt16());
                    track.DataAddr.SetValue(reader.ReadUInt32());
                    track.DataAddrFallback.SetValue(reader.ReadUInt32());
                    bufferData.Tracks.Add(track);
                }

                var dataCount = reader.ReadVLQInt32();
                bufferData.Data.SetValue(reader.ReadBytes(dataCount));
                bufferData.NumFrames.SetValue(reader.ReadBytes(dataCount));
                bufferData.Dt.SetValue(reader.ReadBytes(dataCount));
            }

            CR2WJsonTool.WriteCR2W(cr2w, outputPath);
            return true;
        }
    }
}

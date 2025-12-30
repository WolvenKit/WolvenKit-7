using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using CommandLine;
using WolvenKit.Common.Extensions;
using WolvenKit.CR2W;
using WolvenKit.CR2W.Types;
using WolvenKit.CR2W.JSON;
using WolvenKit.CR2W.SRT;

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
    }
}

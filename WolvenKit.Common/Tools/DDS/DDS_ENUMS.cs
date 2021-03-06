namespace WolvenKit.Common.Tools.DDS
{
   

   //public enum ETextureFormat
   // {
   //     /*TEXFMT_A8 = 0x0,
   //     TEXFMT_A8_Scaleform = 0x1,
   //     TEXFMT_L8 = 0x2,
   //     TEXFMT_R8G8B8X8 = 0x3,*/
   //     TEXFMT_R8G8B8A8 = 0x4,
   //     /*TEXFMT_A8L8 = 0x5,
   //     TEXFMT_Uint_16_norm = 0x6,
   //     TEXFMT_Uint_16 = 0x7,
   //     TEXFMT_Uint_32 = 0x8,
   //     TEXFMT_R32G32_Uint = 0x9,
   //     TEXFMT_R16G16_Uint = 0xA,
   //     TEXFMT_Float_R10G10B10A2 = 0xB,*/
   //     TEXFMT_Float_R16G16B16A16 = 0xC,
   //     /*TEXFMT_Float_R11G11B10 = 0xD,
   //     TEXFMT_Float_R16G16 = 0xE,
   //     TEXFMT_Float_R32G32 = 0xF,*/
   //     TEXFMT_Float_R32G32B32A32 = 0x10,
   //     /*TEXFMT_Float_R32 = 0x11,
   //     TEXFMT_Float_R16 = 0x12,
   //     TEXFMT_D24S8 = 0x13,
   //     TEXFMT_D24FS8 = 0x14,
   //     TEXFMT_D32F = 0x15,
   //     TEXFMT_D16U = 0x16,*/
   //     TEXFMT_BC1 = 0x17,
   //     TEXFMT_BC2 = 0x18,
   //     TEXFMT_BC3 = 0x19,
   //     TEXFMT_BC4 = 0x1A,
   //     TEXFMT_BC5 = 0x1B,
   //     TEXFMT_BC6H = 0x1C,
   //     TEXFMT_BC7 = 0x1D,
   //     /*TEXFMT_R8_Uint = 0x1E,
   //     TEXFMT_NULL = 0x1F,
   //     TEXFMT_Max = 0x20,*/
   // };



    /*public enum ETextureFormat
    {
        Format_RGB = 0,
        Format_DXT1 = 1,
        Format_DXT1a = 2,
        Format_DXT3 = 3,
        Format_DXT5 = 4,
        Format_DXT5n = 5,
        Format_BC4 = 6,
        Format_BC5 = 7
    }*/


    /*
    //DX10 formats.
    public uint Format_BC1 = Format_DXT1;
    public uint Format_BC1a = Format_DXT1a;
    public uint Format_BC2 = Format_DXT3;
    public uint Format_BC3 = Format_DXT5;
    public uint Format_BC3n = Format_DXT5n;
    public uint Format_BC4 = 6; // ATI1
    public uint Format_BC5 = 7; // 3DC, ATI2
    */

    public enum D3D10_RESOURCE_DIMENSION
    {
        D3D10_RESOURCE_DIMENSION_UNKNOWN,
        D3D10_RESOURCE_DIMENSION_BUFFER,
        D3D10_RESOURCE_DIMENSION_TEXTURE1D,
        D3D10_RESOURCE_DIMENSION_TEXTURE2D,
        D3D10_RESOURCE_DIMENSION_TEXTURE3D
    }

    
    public enum DXGI_FORMAT
    {
        DXGI_FORMAT_UNKNOWN,
        DXGI_FORMAT_R32G32B32A32_TYPELESS,
        DXGI_FORMAT_R32G32B32A32_FLOAT,
        DXGI_FORMAT_R32G32B32A32_UINT,
        DXGI_FORMAT_R32G32B32A32_SINT,
        DXGI_FORMAT_R32G32B32_TYPELESS,
        DXGI_FORMAT_R32G32B32_FLOAT,
        DXGI_FORMAT_R32G32B32_UINT,
        DXGI_FORMAT_R32G32B32_SINT,
        DXGI_FORMAT_R16G16B16A16_TYPELESS,
        DXGI_FORMAT_R16G16B16A16_FLOAT,
        DXGI_FORMAT_R16G16B16A16_UNORM,
        DXGI_FORMAT_R16G16B16A16_UINT,
        DXGI_FORMAT_R16G16B16A16_SNORM,
        DXGI_FORMAT_R16G16B16A16_SINT,
        DXGI_FORMAT_R32G32_TYPELESS,
        DXGI_FORMAT_R32G32_FLOAT,
        DXGI_FORMAT_R32G32_UINT,
        DXGI_FORMAT_R32G32_SINT,
        DXGI_FORMAT_R32G8X24_TYPELESS,
        DXGI_FORMAT_D32_FLOAT_S8X24_UINT,
        DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS,
        DXGI_FORMAT_X32_TYPELESS_G8X24_UINT,
        DXGI_FORMAT_R10G10B10A2_TYPELESS,
        DXGI_FORMAT_R10G10B10A2_UNORM,
        DXGI_FORMAT_R10G10B10A2_UINT,
        DXGI_FORMAT_R11G11B10_FLOAT,
        DXGI_FORMAT_R8G8B8A8_TYPELESS,
        DXGI_FORMAT_R8G8B8A8_UNORM,
        DXGI_FORMAT_R8G8B8A8_UNORM_SRGB,
        DXGI_FORMAT_R8G8B8A8_UINT,
        DXGI_FORMAT_R8G8B8A8_SNORM,
        DXGI_FORMAT_R8G8B8A8_SINT,
        DXGI_FORMAT_R16G16_TYPELESS,
        DXGI_FORMAT_R16G16_FLOAT,
        DXGI_FORMAT_R16G16_UNORM,
        DXGI_FORMAT_R16G16_UINT,
        DXGI_FORMAT_R16G16_SNORM,
        DXGI_FORMAT_R16G16_SINT,
        DXGI_FORMAT_R32_TYPELESS,
        DXGI_FORMAT_D32_FLOAT,
        DXGI_FORMAT_R32_FLOAT,
        DXGI_FORMAT_R32_UINT,
        DXGI_FORMAT_R32_SINT,
        DXGI_FORMAT_R24G8_TYPELESS,
        DXGI_FORMAT_D24_UNORM_S8_UINT,
        DXGI_FORMAT_R24_UNORM_X8_TYPELESS,
        DXGI_FORMAT_X24_TYPELESS_G8_UINT,
        DXGI_FORMAT_R8G8_TYPELESS,
        DXGI_FORMAT_R8G8_UNORM,
        DXGI_FORMAT_R8G8_UINT,
        DXGI_FORMAT_R8G8_SNORM,
        DXGI_FORMAT_R8G8_SINT,
        DXGI_FORMAT_R16_TYPELESS,
        DXGI_FORMAT_R16_FLOAT,
        DXGI_FORMAT_D16_UNORM,
        DXGI_FORMAT_R16_UNORM,
        DXGI_FORMAT_R16_UINT,
        DXGI_FORMAT_R16_SNORM,
        DXGI_FORMAT_R16_SINT,
        DXGI_FORMAT_R8_TYPELESS,
        DXGI_FORMAT_R8_UNORM,
        DXGI_FORMAT_R8_UINT,
        DXGI_FORMAT_R8_SNORM,
        DXGI_FORMAT_R8_SINT,
        DXGI_FORMAT_A8_UNORM,
        DXGI_FORMAT_R1_UNORM,
        DXGI_FORMAT_R9G9B9E5_SHAREDEXP,
        DXGI_FORMAT_R8G8_B8G8_UNORM,
        DXGI_FORMAT_G8R8_G8B8_UNORM,
        DXGI_FORMAT_BC1_TYPELESS,
        DXGI_FORMAT_BC1_UNORM,
        DXGI_FORMAT_BC1_UNORM_SRGB,
        DXGI_FORMAT_BC2_TYPELESS,
        DXGI_FORMAT_BC2_UNORM,
        DXGI_FORMAT_BC2_UNORM_SRGB,
        DXGI_FORMAT_BC3_TYPELESS,
        DXGI_FORMAT_BC3_UNORM,
        DXGI_FORMAT_BC3_UNORM_SRGB,
        DXGI_FORMAT_BC4_TYPELESS,
        DXGI_FORMAT_BC4_UNORM,
        DXGI_FORMAT_BC4_SNORM,
        DXGI_FORMAT_BC5_TYPELESS,
        DXGI_FORMAT_BC5_UNORM,
        DXGI_FORMAT_BC5_SNORM,
        DXGI_FORMAT_B5G6R5_UNORM,
        DXGI_FORMAT_B5G5R5A1_UNORM,
        DXGI_FORMAT_B8G8R8A8_UNORM,
        DXGI_FORMAT_B8G8R8X8_UNORM,
        DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM,
        DXGI_FORMAT_B8G8R8A8_TYPELESS,
        DXGI_FORMAT_B8G8R8A8_UNORM_SRGB,
        DXGI_FORMAT_B8G8R8X8_TYPELESS,
        DXGI_FORMAT_B8G8R8X8_UNORM_SRGB,
        DXGI_FORMAT_BC6H_TYPELESS,
        DXGI_FORMAT_BC6H_UF16,
        DXGI_FORMAT_BC6H_SF16,
        DXGI_FORMAT_BC7_TYPELESS,
        DXGI_FORMAT_BC7_UNORM,
        DXGI_FORMAT_BC7_UNORM_SRGB,
        DXGI_FORMAT_AYUV,
        DXGI_FORMAT_Y410,
        DXGI_FORMAT_Y416,
        DXGI_FORMAT_NV12,
        DXGI_FORMAT_P010,
        DXGI_FORMAT_P016,
        DXGI_FORMAT_420_OPAQUE,
        DXGI_FORMAT_YUY2,
        DXGI_FORMAT_Y210,
        DXGI_FORMAT_Y216,
        DXGI_FORMAT_NV11,
        DXGI_FORMAT_AI44,
        DXGI_FORMAT_IA44,
        DXGI_FORMAT_P8,
        DXGI_FORMAT_A8P8,
        DXGI_FORMAT_B4G4R4A4_UNORM,
        DXGI_FORMAT_P208,
        DXGI_FORMAT_V208,
        DXGI_FORMAT_V408,
        DXGI_FORMAT_FORCE_UINT
    }
}

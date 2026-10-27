using System;
using System.Runtime.InteropServices;
using System.Text;
namespace NeonEmber {
[StructLayout(LayoutKind.Sequential)] public struct V { public float X,Y; public V(float x,float y){X=x;Y=y;} }
[StructLayout(LayoutKind.Sequential)] public struct Rect { public float X,Y,W,H; public Rect(float x,float y,float w,float h){X=x;Y=y;W=w;H=h;} }
[StructLayout(LayoutKind.Sequential)] public struct Col { public byte R,G,B,A; public Col(int r,int g,int b,int a){R=(byte)r;G=(byte)g;B=(byte)b;A=(byte)a;} public Col Alpha(float a){return new Col(R,G,B,(int)(Math.Max(0,Math.Min(1,a))*255));} }
[StructLayout(LayoutKind.Sequential)] public struct Texture { public uint Id; public int W,H,Mipmaps,Format; }
[StructLayout(LayoutKind.Sequential)] public struct Target { public uint Id; public Texture Texture,Depth; }
[StructLayout(LayoutKind.Sequential)] public struct Camera2D { public V Offset,Target; public float Rotation,Zoom; }
[StructLayout(LayoutKind.Sequential)] public struct Font { public int BaseSize,GlyphCount,GlyphPadding; public Texture Texture; public IntPtr Recs,Glyphs; }
[StructLayout(LayoutKind.Sequential)] public struct Img { public IntPtr Data; public int W,H,Mipmaps,Format; }
[StructLayout(LayoutKind.Sequential)] public struct Wave { public uint Frames,Rate,Size,Channels; public IntPtr Data; }
[StructLayout(LayoutKind.Sequential)] public struct AudioStream { public IntPtr Buffer,Processor; public uint Rate,Size,Channels; }
[StructLayout(LayoutKind.Sequential)] public struct Sound { public AudioStream Stream; public uint Frames; }
public static class R {
 const string D="raylib.dll";
 public static byte[] U(string s){return Encoding.UTF8.GetBytes(s+"\0");}
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void SetConfigFlags(uint flags);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void InitWindow(int w,int h,byte[] title);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void CloseWindow();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] public static extern bool WindowShouldClose();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] public static extern bool IsWindowFocused();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void SetExitKey(int k);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void SetWindowMinSize(int w,int h);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void ToggleBorderlessWindowed();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void SetTargetFPS(int fps);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern int GetFPS();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern float GetFrameTime();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern int GetScreenWidth();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern int GetScreenHeight();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern int GetRenderWidth();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern int GetRenderHeight();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void SetTraceLogLevel(int level);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void BeginDrawing();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void EndDrawing();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void ClearBackground(Col c);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern Target LoadRenderTexture(int w,int h);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void UnloadRenderTexture(Target t);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void BeginTextureMode(Target t);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void EndTextureMode();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void BeginMode2D(Camera2D camera);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void EndMode2D();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void DrawTexturePro(Texture t,Rect source,Rect dest,V origin,float rotation,Col tint);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void SetTextureFilter(Texture t,int filter);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void DrawRectangleRec(Rect r,Col c);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void DrawRectangleRounded(Rect r,float roundness,int segments,Col c);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void DrawRectangleRoundedLinesEx(Rect r,float roundness,int segments,float thick,Col c);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void DrawRectangleLinesEx(Rect r,float thick,Col c);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void DrawLineEx(V a,V b,float thick,Col c);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void DrawCircleV(V p,float radius,Col c);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void DrawCircleLinesV(V p,float radius,Col c);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void DrawRing(V p,float inner,float outer,float start,float end,int seg,Col c);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void DrawPoly(V p,int sides,float radius,float rotation,Col c);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void DrawPolyLinesEx(V p,int sides,float radius,float rotation,float thickness,Col c);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern Font LoadFontFromMemory(byte[] type,byte[] data,int dataSize,int size,int[] codepoints,int count);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern Font GetFontDefault();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void UnloadFont(Font f);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void DrawTextEx(Font f,byte[] text,V pos,float size,float spacing,Col c);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern V MeasureTextEx(Font f,byte[] text,float size,float spacing);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern V GetMousePosition();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] public static extern bool IsMouseButtonPressed(int k);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] public static extern bool IsKeyPressed(int k);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] public static extern bool IsKeyDown(int k);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void TakeScreenshot(byte[] path);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern Img LoadImageFromTexture(Texture t);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void ImageFlipVertical(ref Img image);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] public static extern bool ExportImage(Img image,byte[] path);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void UnloadImage(Img image);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void InitAudioDevice();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] public static extern bool IsAudioDeviceReady();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void CloseAudioDevice();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern Sound LoadSoundFromWave(Wave wave);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void PlaySound(Sound sound);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void SetSoundVolume(Sound sound,float volume);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] public static extern void UnloadSound(Sound sound);
}
}

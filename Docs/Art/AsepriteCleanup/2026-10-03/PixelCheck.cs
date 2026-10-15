using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class ArtCleanupPixelCheck {
 static byte[] Read(Bitmap b) {
  var data=b.LockBits(new Rectangle(0,0,b.Width,b.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
  try {
   byte[] result=new byte[b.Width*b.Height*4];
   for(int y=0;y<b.Height;y++) Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),result,y*b.Width*4,b.Width*4);
   return result;
  } finally { b.UnlockBits(data); }
 }
 public static double[] Compare(string before,string after) {
  using(var a=new Bitmap(before)) using(var b=new Bitmap(after)) {
   if(a.Width!=b.Width||a.Height!=b.Height) throw new Exception("Image dimensions changed: "+before);
   var aa=Read(a);var bb=Read(b);long alphaErrors=0,changed=0,visible=0;int maxError=0;
   double error=0,noiseBefore=0,noiseAfter=0;long samples=0;
   for(int p=0;p<aa.Length;p+=4) {
    if(aa[p+3]!=bb[p+3]) alphaErrors++;
    if(aa[p+3]==0) continue;
    visible++; bool diff=false;
    for(int c=0;c<3;c++) {int d=Math.Abs(aa[p+c]-bb[p+c]);error+=d;maxError=Math.Max(maxError,d);if(d>0)diff=true;}
    if(diff)changed++;
   }
   int row=a.Width*4;
   for(int y=1;y<a.Height-1;y++) for(int x=1;x<a.Width-1;x++) {
    int p=y*row+x*4;
    if(aa[p+3]<240||aa[p-row+3]<240||aa[p+row+3]<240||aa[p-4+3]<240||aa[p+4+3]<240)continue;
    bool flat=true;
    for(int c=0;c<3;c++) {
     int lo=Math.Min(aa[p+c],Math.Min(Math.Min(aa[p-row+c],aa[p+row+c]),Math.Min(aa[p-4+c],aa[p+4+c])));
     int hi=Math.Max(aa[p+c],Math.Max(Math.Max(aa[p-row+c],aa[p+row+c]),Math.Max(aa[p-4+c],aa[p+4+c])));
     if(hi-lo>16){flat=false;break;}
    }
    if(!flat)continue;
    samples++;
    for(int c=0;c<3;c++) {
     noiseBefore+=Math.Abs(aa[p+c]-(aa[p-row+c]+aa[p+row+c]+aa[p-4+c]+aa[p+4+c])/4.0);
     noiseAfter+=Math.Abs(bb[p+c]-(bb[p-row+c]+bb[p+row+c]+bb[p-4+c]+bb[p+4+c])/4.0);
    }
   }
   return new double[]{a.Width,a.Height,alphaErrors,changed,visible,maxError,error/Math.Max(visible*3,1),noiseBefore/Math.Max(samples*3,1),noiseAfter/Math.Max(samples*3,1),samples};
  }
 }
}

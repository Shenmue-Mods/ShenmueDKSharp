using System.Drawing;

namespace ShenmueDKSharp.Files.Images._PVRT.WQuantizer
{
    public interface IWuQuantizer
    {
        Image QuantizeImage(Bitmap image, int alphaThreshold, int alphaFader);
    }
}
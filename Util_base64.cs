using System.Drawing;

namespace ASP.NET_Application_Test.Utils
{
    public class Util_base64
    {
        internal static string ConvertImageToBase64(string image_path)
        {
            using (Image image = Image.FromFile(image_path))
            {
                using (MemoryStream m = new MemoryStream())
                {
                    image.Save(m, image.RawFormat);
                    byte[] imageBytes = m.ToArray();
                    return Convert.ToBase64String(imageBytes);
                };
            };

        }
    }
}

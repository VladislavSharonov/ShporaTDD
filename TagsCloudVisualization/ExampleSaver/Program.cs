using System.Drawing.Drawing2D;
using System.Globalization;
using TagsCloudVisualization;

namespace ExampleSaver
{
    internal static class Program
    {
        private static void Main()
        {
            var size = new Size(400, 400);


            using var brush = new SolidBrush(Color.Orange);
            using var pen = new Pen(brush, 1) { Alignment = PenAlignment.Inset };


            for (var i = 0; i < 2; i++)
            {
                var layouterSetupValue = 0.01 * Math.Pow(100, i);
                var layouter = new CircularCloudLayouter(new Point(size.Width / 2, size.Height / 2), layouterSetupValue,
                    layouterSetupValue);
                using var img = new Bitmap(size.Width, size.Height);
                using var graphics = Graphics.FromImage(img);
                graphics.Clear(Color.SlateBlue);
                for (int j = 0; j < 100; j++)
                {
                    var rect = layouter.PutNextRectangle(new Size(10 + Random.Shared.Next(20),
                        10 + Random.Shared.Next(20)));
                    graphics.DrawRectangle(pen, rect);
                }

                var localPath = string.Format(CultureInfo.InvariantCulture,
                    "../../../../Resources/imgs/Density{0}_AngleStep{1}.jpg", layouter.Density, layouter.AngleStep);
                img.Save(Path.GetFullPath(localPath));
                Console.WriteLine($"Image saved: {localPath}");
                layouter.Clear();
            }
        }
    }
}
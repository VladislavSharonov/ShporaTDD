using System.Drawing.Drawing2D;
using FluentAssertions;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using TagsCloudVisualization;

namespace TagsCloudVisualizationTests;

public class CircularCloudLayouterTests
{
    private Point center = new Point(500, 500);
    private CircularCloudLayouter layouter;

    [SetUp]
    public void Setup()
    {
        layouter = new CircularCloudLayouter(new Point(500, 500));
    }

    [TearDown]
    public void TearDown()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
        {
            using var bitmap = new Bitmap(center.X * 2, center.Y * 2);
            using var graphics = Graphics.FromImage(bitmap);
            using var brush = new SolidBrush(Color.Orange);
            using var pen = new Pen(brush, 1) { Alignment = PenAlignment.Inset };
            for (var i = 0; i < layouter.Rectangles.Count; i++)
                graphics.DrawRectangle(pen, layouter.Rectangles[i]);

            var testName = TestContext.CurrentContext.Test.Name;
            var debugPath = TestContext.CurrentContext.TestDirectory;

            var path = Path.Combine(debugPath, testName) + @"_Failed.jpg";
            bitmap.Save(path);

            TestContext.Error.WriteLine($"Tag cloud visualization saved to file {path}");
        }
    }

    [TestCase(0, 0)]
    [TestCase(1, 0)]
    [TestCase(0, 1)]
    [TestCase(-1, 1)]
    [TestCase(1, -1)]
    [TestCase(-1, -1)]
    public void PutNextRectangle_ThrowArgumentException_OnNonPositiveSize(int width, int height)
    {
        var action = () => layouter.PutNextRectangle(new Size(width, height));
        action.Should().Throw<ArgumentException>();
    }

    [Test]
    public void PutNextRectangle_ThrowException_OnHaveNoSpace()
    {
        var layouter = new CircularCloudLayouter(this.layouter.Center, int.MaxValue, int.MaxValue);
        var action = () =>
        {
            for (var i = 0; i < 100; i++)
                layouter.PutNextRectangle(new Size(int.MaxValue, int.MaxValue));
        };
        action.Should().Throw<Exception>().WithMessage("There is no place for the rectangle");
    }

    [Test]
    public void PutNextRectangle_NotThrowArgumentException_OnPositiveSize()
    {
        Action act = () => layouter.PutNextRectangle(new Size(1, 1));
        act.Should().NotThrow<ArgumentException>();
    }

    [Test]
    public void PutNextRectangle_AddOne()
    {
        layouter.PutNextRectangle(new Size(40, 60));
        layouter.Rectangles.Count.Should().Be(1);
    }

    [Test]
    public void PutNextRectangle_FirstInCenter()
    {
        var size = new Size(43, 45);
        layouter.PutNextRectangle(size);
        layouter.Rectangles[0].Location
            .Should()
            .Be(new Point(center.X - size.Width / 2, center.Y - size.Height / 2));
    }

    [TestCase(81, 97, TestName = "Added with correct size on odd numbers")]
    [TestCase(30, 14, TestName = "Added with correct size on even numbers")]
    public void PutNextRectangle_AddedWithCorrectSize(int width, int height)
    {
        var size = new Size(width, height);
        layouter.PutNextRectangle(size);
        layouter.Rectangles[0].Size.Should().Be(size);
    }

    [TestCase(-8, -8, 6, 6, ExpectedResult = true, TestName = "Left Top overlap")]
    [TestCase(-26, -26, 6, 6, ExpectedResult = false, TestName = "Left Top not overlap")]
    [TestCase(-8, -2, 6, 6, ExpectedResult = true, TestName = "Left Center overlap")]
    [TestCase(-8, 4, 6, 6, ExpectedResult = true, TestName = "Left Bottom overlap")]
    [TestCase(-26, 26, 6, 6, ExpectedResult = false, TestName = "Left Bottom not overlap")]
    [TestCase(-2, -8, 6, 6, ExpectedResult = true, TestName = "Center Top overlap")]
    [TestCase(-2, -2, 6, 6, ExpectedResult = true, TestName = "Inside overlap")]
    [TestCase(-2, 4, 6, 6, ExpectedResult = true, TestName = "Center Bottom overlap")]
    [TestCase(4, -8, 6, 6, ExpectedResult = true, TestName = "Right Top overlap")]
    [TestCase(26, -26, 6, 6, ExpectedResult = false, TestName = "Right Top not overlap")]
    [TestCase(4, -2, 6, 6, ExpectedResult = true, TestName = "Right Center overlap")]
    [TestCase(4, 4, 6, 6, ExpectedResult = true, TestName = "Right Bottom overlap")]
    [TestCase(26, 26, 6, 6, ExpectedResult = false, TestName = "Right Bottom not overlap")]
    [TestCase(-10, -10, 20, 40, ExpectedResult = true, TestName = "Area inside targer area")]
    [TestCase(-6, -6, 12, 12, ExpectedResult = true, TestName = "Overlap the same area")]
    public bool HasOverlapWith_CorrectAnswer_OnOverlap(int xOffset, int yOffset, int width, int height)
    {
        var staticSize = new Size(12, 12);
        layouter.PutNextRectangle(staticSize);
        var rectangle = new Rectangle(new Point(center.X + xOffset, center.Y + yOffset), new Size(width, height));
        return layouter.HasOverlapWith(rectangle);
    }

    [Test]
    public void PutNextRectangle_IsDefaultСloudDensely()
    {
        const double accuracy = 0.2;

        // The more rectangles, the more accurately repeats the shape of a circle
        for (var i = 0; i < 200; i++)
            layouter.PutNextRectangle(new Size(50, 20));

        var rects = layouter.Rectangles;
        var rectangleArea = rects.Select(x => x.Width * x.Height).Sum();
        var horizontalRadius = Math.Abs(rects.Max(x => x.Right) - rects.Min(x => x.Left)) / 2;
        var verticalRadius = Math.Abs(rects.Max(x => x.Bottom) - rects.Min(x => x.Top)) / 2;
        var boundingElipseArea = Math.PI * horizontalRadius * verticalRadius;
        (Math.Abs(rectangleArea / boundingElipseArea - 1)).Should().BeLessThan(accuracy);
    }


    [Test]
    public void PutNextRectangle_DefaultСloudIsCircle()
    {
        for (var i = 0; i < 13; i++)
            layouter.PutNextRectangle(new Size(50, 50));

        var rects = layouter.Rectangles;
        var horizontalRadius = Math.Abs(rects.Max(x => x.Right) - rects.Min(x => x.Left)) / 2;
        var verticalRadius = Math.Abs(rects.Max(x => x.Bottom) - rects.Min(x => x.Top)) / 2;
        var centers = rects.Select(r => new Point(r.X + r.Width / 2, r.Y + r.Height / 2));

        var distance = (Point p) =>
            Math.Sqrt(Math.Pow(p.X - layouter.Center.X, 2) + Math.Pow(p.Y - layouter.Center.Y, 2));
        centers.All(center => distance(center) < Math.Max(horizontalRadius, verticalRadius)).Should().BeTrue();
    }

    [Test]
    public void Clear_Empty_AfterClear()
    {
        for (var i = 0; i < 5; i++)
            layouter.PutNextRectangle(new Size(50, 50));

        layouter.Clear();
        layouter.Rectangles.Count.Should().Be(0);
    }
}
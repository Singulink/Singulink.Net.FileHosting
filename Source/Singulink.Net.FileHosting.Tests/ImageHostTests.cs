using System;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Singulink.IO;

namespace Singulink.Net.FileHosting.Tests
{
    [TestClass]
    public class ImageHostTests
    {
        public static readonly IAbsoluteDirectoryPath _assemblyDir = DirectoryPath.GetAssemblyLocation(typeof(ImageHostTests).Assembly);
        public static readonly IAbsoluteFilePath _imageFile = _assemblyDir.CombineDirectory("Images").CombineFile("test-1024x768.jpg");
        public static readonly IAbsoluteDirectoryPath _hostingDir = _assemblyDir.CombineDirectory("HostingRoot");
        public static readonly ImageHost _host = new ImageHost(_hostingDir);

        [TestMethod]
        public void ImageSourceValidation()
        {
            ResetHostingDir();

            using var stream = _imageFile.OpenStream();

            var ex = Assert.ThrowsException<ArgumentException>(() => {
                _host.Add(stream, new ImageOptions() {
                    ValidateSource = i => i.Width > 500 || i.Height > 500 ? "Image is too large" : null,
                });
            });

            ex.Message.ShouldBe("Image is too large");
        }

        [TestMethod]
        public void Quality()
        {
            ResetHostingDir();

            using var stream = _imageFile.OpenStream();

            var id = _host.Add(stream, new ImageOptions() {
                Size = new Size(300, 300),
                Quality = 100,
            });

            _host.AddSize(id, "75", new ImageOptions() {
                Size = new Size(300, 300),
                Quality = 75,
            });

            _host.AddSize(id, "50", new ImageOptions() {
                Size = new Size(300, 300),
                Quality = 50,
            });

            _host.AddSize(id, "25", new ImageOptions() {
                Size = new Size(300, 300),
                Quality = 25,
            });

            var i100 = _host.GetImagePath(id);
            var i75 = _host.GetImagePath(id, "75");
            var i50 = _host.GetImagePath(id, "50");
            var i25 = _host.GetImagePath(id, "25");

            i100.Length.ShouldBeGreaterThan(i75.Length);
            i75.Length.ShouldBeGreaterThan(i50.Length);
            i50.Length.ShouldBeGreaterThan(i25.Length);
        }

        [TestMethod]
        public void NoResize()
        {
            ResetHostingDir();

            using var stream = _imageFile.OpenStream();

            var id = _host.Add(stream, new ImageOptions());

            Assert.ThrowsException<ArgumentException>(() => _host.AddSize(id, "original", new ImageOptions()));

            _host.AddSize(id, "enlarged1", new ImageOptions() {
                Size = new Size(2000, 2000),
            });

            _host.AddSize(id, "enlarged2", new ImageOptions() {
                Size = new Size(2048, 1536),
                ResizeMode = ImageResizeMode.DownsizeAndCover,
            });

            var imagePath = _host.GetImagePath(id);
            imagePath.Exists.ShouldBeTrue();

            using (var image = Image.FromFile(imagePath.PathExport)) {
                image.Width.ShouldBe(1024);
                image.Height.ShouldBe(768);
            }

            var enlarged1Path = _host.GetImagePath(id, "enlarged1");
            enlarged1Path.Exists.ShouldBeTrue();

            using (var enlarged1 = Image.FromFile(enlarged1Path.PathExport)) {
                enlarged1.Width.ShouldBe(1024);
                enlarged1.Height.ShouldBe(768);
            }

            var enlarged2Path = _host.GetImagePath(id, "enlarged2");
            enlarged2Path.Exists.ShouldBeTrue();

            using (var enlarged2 = Image.FromFile(enlarged2Path.PathExport)) {
                enlarged2.Width.ShouldBe(1024);
                enlarged2.Height.ShouldBe(768);
            }

            _host.Delete(id);
            imagePath.Exists.ShouldBeFalse();
            enlarged1Path.Exists.ShouldBeFalse();
            enlarged2Path.Exists.ShouldBeFalse();
        }

        [TestMethod]
        public void Downsize()
        {
            ResetHostingDir();

            using var stream = _imageFile.OpenStream();

            var id = _host.Add(stream, new ImageOptions() {
                Size = new Size(500, 500),
            });

            _host.AddSize(id, "thumbnail", new ImageOptions() {
                Size = new Size(150, 150),
            });

            var imagePath = _host.GetImagePath(id);
            imagePath.Exists.ShouldBeTrue();

            using (var image = Image.FromFile(imagePath.PathExport)) {
                image.Width.ShouldBe(500);
                image.Height.ShouldBe(375);
            }

            var thumbnailPath = _host.GetImagePath(id, "thumbnail");
            imagePath.Exists.ShouldBeTrue();

            using (var thumbnail = Image.FromFile(thumbnailPath.PathExport)) {
                thumbnail.Width.ShouldBe(150);
                thumbnail.Height.ShouldBe(112);
            }

            _host.Delete(id);
            imagePath.Exists.ShouldBeFalse();
            thumbnailPath.Exists.ShouldBeFalse();
        }

        [TestMethod]
        public void DownsizeAndCover()
        {
            ResetHostingDir();

            using var stream = _imageFile.OpenStream();

            var id = _host.Add(stream, new ImageOptions() {
                Size = new Size(500, 500),
                ResizeMode = ImageResizeMode.DownsizeAndCover,
            });

            _host.AddSize(id, "thumbnail", new ImageOptions() {
                Size = new Size(150, 150),
                ResizeMode = ImageResizeMode.DownsizeAndCover,
            });

            var imagePath = _host.GetImagePath(id);
            imagePath.Exists.ShouldBeTrue();

            using (var image = Image.FromFile(imagePath.PathExport)) {
                image.Width.ShouldBe(500);
                image.Height.ShouldBe(500);
            }

            var thumbnailPath = _host.GetImagePath(id, "thumbnail");
            imagePath.Exists.ShouldBeTrue();

            using (var thumbnail = Image.FromFile(thumbnailPath.PathExport)) {
                thumbnail.Width.ShouldBe(150);
                thumbnail.Height.ShouldBe(150);
            }

            _host.Delete(id);
            imagePath.Exists.ShouldBeFalse();
            thumbnailPath.Exists.ShouldBeFalse();
        }

        private static void ResetHostingDir()
        {
            if (_hostingDir.Exists)
                _hostingDir.Delete(true);
        }
    }
}

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
        public static readonly ImageHost _host = new ImageHost(_hostingDir, new ImageHostOptions { DeleteFailureMode = DeleteFailureMode.Throw });

        [TestMethod]
        public void ImageSourceValidation()
        {
            ResetHostingDir();

            using var stream = _imageFile.OpenStream();

            var ex = Assert.ThrowsException<ArgumentException>(() => {
                _host.Add(stream, new ImageOptions {
                    ValidateSource = i => {
                        if (i.Width > 500 || i.Height > 500)
                            throw new ArgumentException("Image is too large");
                    },
                });
            });

            ex.Message.ShouldBe("Image is too large");
        }

        [TestMethod]
        public void Quality()
        {
            ResetHostingDir();

            using var stream = _imageFile.OpenStream();

            var key = _host.Add(stream, new ImageOptions() {
                ImageEditor = ImageEditors.MaxSize(new Size(300, 300), Color.White),
                JpegQuality = 100,
            });

            _host.AddSize(key, "75", new ImageOptions() {
                ImageEditor = ImageEditors.MaxSize(new Size(300, 300), Color.White),
                JpegQuality = 75,
            });

            _host.AddSize(key, "50", new ImageOptions() {
                ImageEditor = ImageEditors.MaxSize(new Size(300, 300), Color.White),
                JpegQuality = 50,
            });

            _host.AddSize(key, "25", new ImageOptions() {
                ImageEditor = ImageEditors.MaxSize(new Size(300, 300), Color.White),
                JpegQuality = 25,
            });

            var i100 = _host.GetAbsoluteImagePath(key);
            var i75 = _host.GetAbsoluteImagePath(key, "75");
            var i50 = _host.GetAbsoluteImagePath(key, "50");
            var i25 = _host.GetAbsoluteImagePath(key, "25");

            i100.Length.ShouldBeGreaterThan(i75.Length);
            i75.Length.ShouldBeGreaterThan(i50.Length);
            i50.Length.ShouldBeGreaterThan(i25.Length);
        }

        [TestMethod]
        public void NoResize()
        {
            ResetHostingDir();

            using var stream = _imageFile.OpenStream();

            var key = _host.Add(stream, new ImageOptions());

            Assert.ThrowsException<ArgumentException>(() => _host.AddSize(key, "original", new ImageOptions()));

            _host.AddSize(key, "enlarged1", new ImageOptions() {
                ImageEditor = ImageEditors.MaxSize(new Size(2000, 2000), Color.White),
            });

            _host.AddSize(key, "enlarged2", new ImageOptions() {
                ImageEditor = ImageEditors.Crop(new Size(2048, 1536), Color.White),
            });

            using (var image = Image.FromFile(_host.GetAbsoluteImagePath(key).PathExport)) {
                image.Width.ShouldBe(1024);
                image.Height.ShouldBe(768);
            }

            using (var enlarged1 = Image.FromFile(_host.GetAbsoluteImagePath(key, "enlarged1").PathExport)) {
                enlarged1.Width.ShouldBe(1024);
                enlarged1.Height.ShouldBe(768);
            }

            using (var enlarged2 = Image.FromFile(_host.GetAbsoluteImagePath(key, "enlarged2").PathExport)) {
                enlarged2.Width.ShouldBe(1024);
                enlarged2.Height.ShouldBe(768);
            }
        }

        [TestMethod]
        public void MaxSize()
        {
            ResetHostingDir();

            using var stream = _imageFile.OpenStream();

            var key = _host.Add(stream, new ImageOptions() {
                ImageEditor = ImageEditors.MaxSize(new Size(500, 500), Color.White),
            });

            _host.AddSize(key, "thumbnail", new ImageOptions() {
                ImageEditor = ImageEditors.MaxSize(new Size(150, 150), Color.White),
            });

            using (var image = Image.FromFile(_host.GetAbsoluteImagePath(key).PathExport)) {
                image.Width.ShouldBe(500);
                image.Height.ShouldBe(375);
            }

            using (var thumbnail = Image.FromFile(_host.GetAbsoluteImagePath(key, "thumbnail").PathExport)) {
                thumbnail.Width.ShouldBe(150);
                thumbnail.Height.ShouldBe(112);
            }
        }

        [TestMethod]
        public void Crop()
        {
            ResetHostingDir();

            using var stream = _imageFile.OpenStream();

            var key = _host.Add(stream, new ImageOptions() {
                ImageEditor = ImageEditors.Crop(new Size(500, 500), Color.White),
            });

            _host.AddSize(key, "thumbnail", new ImageOptions() {
                ImageEditor = ImageEditors.Crop(new Size(150, 150), Color.White),
            });

            using (var image = Image.FromFile(_host.GetAbsoluteImagePath(key).PathExport)) {
                image.Width.ShouldBe(500);
                image.Height.ShouldBe(500);
            }

            using (var thumbnail = Image.FromFile(_host.GetAbsoluteImagePath(key, "thumbnail").PathExport)) {
                thumbnail.Width.ShouldBe(150);
                thumbnail.Height.ShouldBe(150);
            }
        }

        [TestMethod]
        public void Pad()
        {
            ResetHostingDir();

            using var stream = _imageFile.OpenStream();

            var key = _host.Add(stream, new ImageOptions() {
                ImageEditor = ImageEditors.Pad(new Size(500, 500), Color.White),
            });

            _host.AddSize(key, "thumbnail", new ImageOptions() {
                ImageEditor = ImageEditors.Pad(new Size(150, 150), Color.White),
            });

            using (var image = Image.FromFile(_host.GetAbsoluteImagePath(key).PathExport)) {
                image.Width.ShouldBe(500);
                image.Height.ShouldBe(500);
            }

            using (var thumbnail = Image.FromFile(_host.GetAbsoluteImagePath(key, "thumbnail").PathExport)) {
                thumbnail.Width.ShouldBe(150);
                thumbnail.Height.ShouldBe(150);
            }
        }

        [TestMethod]
        public void CreateAndDelete()
        {
            ResetHostingDir();

            using var stream = _imageFile.OpenStream();

            var key = _host.Add(stream, new ImageOptions() {
                ImageEditor = ImageEditors.MaxSize(new Size(500, 500), Color.White),
            });

            _host.AddSize(key, "thumbnail", new ImageOptions() {
                ImageEditor = ImageEditors.MaxSize(new Size(150, 150), Color.White),
            });

            var imagePath = _host.GetAbsoluteImagePath(key);
            imagePath.Exists.ShouldBeTrue();

            var thumbnailPath = _host.GetAbsoluteImagePath(key, "thumbnail");
            imagePath.Exists.ShouldBeTrue();

            _host.Delete(key.Id);
            imagePath.Exists.ShouldBeFalse();
            thumbnailPath.Exists.ShouldBeFalse();

            imagePath.ParentDirectory.Exists.ShouldBeFalse();
            imagePath.ParentDirectory.ParentDirectory!.Exists.ShouldBeFalse();
            imagePath.ParentDirectory.ParentDirectory!.ParentDirectory!.Exists.ShouldBeTrue();
        }

        private static void ResetHostingDir()
        {
            if (_hostingDir.Exists)
                _hostingDir.Delete(true);
        }
    }
}
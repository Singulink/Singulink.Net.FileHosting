# Singulink.Net.FileHosting

[![Chat on Discord](https://img.shields.io/discord/906246067773923490)](https://discord.gg/EkQhJFsBu6)
[![View nuget packages](https://img.shields.io/nuget/v/Singulink.Net.FileHosting.svg)](https://www.nuget.org/packages/Singulink.Net.FileHosting/)
[![Build and Test](https://github.com/Singulink/Singulink.Net.FileHosting/workflows/build%20and%20test/badge.svg)](https://github.com/Singulink/Singulink.Net.FileHosting/actions?query=workflow%3A%22build+and+test%22)

**Singulink.Net.FileHosting** provides file and image storage functionality like image resizing and thumbnail generation suitable for hosting scenarios where users upload files. Source image parameters, such as size, can be validated prior to fully loading and processing the image to prevent easy denial-of-service attacks like uploading massive resolution JPEG files that are only 2MB in size but allocate 1GB+ memory when loaded.

### About Singulink

We are a small team of engineers and designers dedicated to building beautiful, functional and well-engineered software solutions. We offer very competitive rates as well as fixed-price contracts and welcome inquiries to discuss any custom development / project support needs you may have.

This package is part of our **Singulink Libraries** collection. Visit https://github.com/Singulink to see our full list of publicly available libraries and other open-source projects.

## Installation

The package is available on NuGet - simply install the `Singulink.Net.FileHosting` package.

**Supported Runtimes**: Anywhere .NET Standard 2.1+ is supported, including:
- .NET Core 3.0+
- Mono 6.4+
- Xamarin.iOS 12.16+
- Xamarin.Android 10.0+

## API

You can view the API on [FuGet](https://www.fuget.org/packages/Singulink.Net.FileHosting).

## Project Status

Documentation and development is in progress. Poke around the source code or check out our other projects for now!

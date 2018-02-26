﻿using ImageMagick;
using System.Collections.Immutable;

namespace Advobot.Core.Interfaces
{
	public interface IImageResizerArguments
	{
		ImmutableArray<MagickFormat> ValidFormats { get; }
		long MaxAllowedLengthInBytes { get; }
		int ResizeTries { get; set; }
		Percentage ColorFuzzing { get; set; }
	}
}
using System;
using System.IO;

namespace Nez
{
	public static class FileHelpers
	{
		// From MonoGame.Framework.FileHelpers
		public static readonly char BackwardSlash = '\\';
		public static readonly char ForwardSlash = '\\';
		public static readonly char Separator = (Path.DirectorySeparatorChar);
		public static readonly char NotSeparator = ((Path.DirectorySeparatorChar == BackwardSlash) ? ForwardSlash : BackwardSlash);

		public static string NormalizeFilePathSeparators(string path)
		{
			return path.Replace(NotSeparator, Separator);
		}
	}
}


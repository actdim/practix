using System;
using System.Globalization;

namespace ActDim.Emitron.Tests
{
	/// <summary>
	/// Temporarily overrides <see cref="CultureInfo.CurrentCulture"/> and <see cref="CultureInfo.CurrentUICulture"/>
	/// and restores the previous values on dispose.
	/// </summary>
	/// <remarks>
	/// Interpolated strings format numbers and dates with the current culture. CI runners use the invariant
	/// culture (<c>LANG=C.UTF-8</c>), where <c>P0</c> renders <c>"15 %"</c> instead of <c>"15%"</c>,
	/// so culture-sensitive assertions must pin the culture explicitly.
	/// </remarks>
	internal sealed class CultureScope : IDisposable
	{
		private readonly CultureInfo _culture;
		private readonly CultureInfo _uiCulture;

		public CultureScope(string name)
		{
			_culture = CultureInfo.CurrentCulture;
			_uiCulture = CultureInfo.CurrentUICulture;
			var culture = CultureInfo.GetCultureInfo(name);
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = culture;
		}

		public void Dispose()
		{
			CultureInfo.CurrentCulture = _culture;
			CultureInfo.CurrentUICulture = _uiCulture;
		}
	}
}

using System;
using System.Linq;
using ActDim.Practix.Threading;
using Ardalis.GuardClauses;
namespace ActDim.Practix
{
    /// <summary>
    /// 
    /// </summary>
	public class ShortId: IDisposable
	{
		// Alpha-numeric
		public const string DefaultCharSet = "0123456789abcdefghjlkmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXY"; // _

		private readonly ThreadSafe<Random, (object SyncRoot, Random GlobalRandom)> _random;

		private string _charSet;

		private readonly object _syncRoot = new();

		private readonly Func<int, int, int> _rnd;

		public ShortId() : this(Guid.NewGuid().GetHashCode() & int.MaxValue)
		{

		}

		public ShortId(string characters) : this(characters, Guid.NewGuid().GetHashCode() & int.MaxValue)
		{

		}		

		/// <summary>
		/// 
		/// </summary>
		/// <param name="seed">The seed for the random number generator</param>
		public ShortId(int seed): this(DefaultCharSet, seed)
		{
			
		}

		public ShortId(string characters, int seed)
		{
			SetCharacters(characters);

			_random = new ThreadSafe<Random, (object SyncRoot, Random GlobalRandom)>(context =>
			{
				int localSeed;
				lock (context.SyncRoot)
				{
					localSeed = context.GlobalRandom.Next();
				}
				return new Random(localSeed);
			}, (new object(), new Random(seed)));

			_rnd = new Func<int, int, int>((int minValue, int maxValue) =>
			{
				return _random.Value.Next(minValue, maxValue);
			});
		}

		/// <summary>
		/// Allows to use custom random number generator (for example Mersenne Twister)
		/// </summary>
		/// <param name="rnd">Random number generator, should be thread-safe</param>
		public ShortId(Func<int, int, int> rnd): this(DefaultCharSet, rnd)
		{
			
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="characters">Character set</param>
		/// <param name="rnd">Random number generator, should be thread-safe</param>
		public ShortId(string characters, Func<int, int, int> rnd)
		{
			SetCharacters(characters);
			Guard.Against.Null(rnd, nameof(rnd));
			_rnd = rnd;
		}

		/// <summary>
		/// Generates a random string of a specified length
		/// </summary>		
		/// <param name="length">The length of the generated string</param>
		/// <returns>A random string</returns>
		public string Generate(int length)
		{
			if (length < 7)
			{
				// The length should be greater than 7
				Guard.Against.OutOfRange(length, nameof(length), 8, int.MaxValue);
			}

			char[] output = new char[length];
			var pool = _charSet;

			for (int i = 0; i < length; i++)
			{
				int charIndex = _rnd(0, pool.Length);
				output[i] = pool[charIndex];
			}

			return new string(output);
		}

		/// <summary>
		/// Changes the character set that id's are generated from
		/// </summary>
		/// <param name="characters">The new character set</param>
		/// <exception cref="InvalidOperationException">Thrown when the new character set is less than 20 characters</exception>					
		public void SetCharacters(string characters)
		{			
			// The character set should not be empty and contain whiteSpace
			Guard.Against.NullOrWhiteSpace(characters, nameof(characters));

			var distinctCharacters = characters.Distinct().ToArray();
			if (distinctCharacters.Length < characters.Length)
			{
				// The character set should not contain any duplicates
				throw new ArgumentException("The character set contains duplicates", nameof(characters));
			}

			if (distinctCharacters.Length < 20)
			{
				// The character set length should be greater than 20
				Guard.Against.InvalidInput(distinctCharacters, nameof(characters), c => c.Length > 20);
			}

			lock (_syncRoot)
			{
				_charSet = new string(distinctCharacters);
			}
		}
				
		#region IDisposable Support

		private bool _isDisposed = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				if (disposing)
				{
					lock (_syncRoot)
					{
						_random.Dispose();
					}
				}

				_isDisposed = true;
			}
		}		
		
		public void Dispose()
		{			
			Dispose(true);
			
			GC.SuppressFinalize(this);
		}

		~ShortId()
		{
			Dispose(false);
		}

		#endregion
	}
}
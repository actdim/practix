using Autofac;
using CanarySystems.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using NUnit.Framework;
using SalientBits.Disposal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Misc.Tests
{
    [TestFixture]
	public class CachingTests : TestContainer
	{
		private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

		private IDisposable Lock()
		{
			_semaphore.Wait();
			return new DisposableAction(() =>
			{
				_semaphore.Release();
			});
		}

		public bool CheckIfFileLocked(string filePath)
		{
			try
			{
				using (File.Open(filePath, FileMode.Open)) { }
			}
			catch (IOException ex)
			{
				// https://docs.microsoft.com/en-us/windows/win32/debug/system-error-codes--0-499-
				const int ERROR_SHARING_VIOLATION = 0x20;
				const int ERROR_LOCK_VIOLATION = 0x21;
				int errorCode = ex.HResult & 0x0000FFFF;
				return errorCode == ERROR_SHARING_VIOLATION || errorCode == ERROR_LOCK_VIOLATION;
			}

			return false;
		}

		public async Task WaitUntilFileIsLocked(string filePath)
		{
			while (CheckIfFileLocked(filePath))
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				await Task.Delay(100);
			}
		}

		public interface IMemoryCacheTestClass
		{
			[Cache(CacheType.InMemory, ExcludeParameterTypes = new[] { typeof(Action) }, SlidingExpiration = "00:00:00.1000000")]
			int ComputeSumWithCallback(int value1, int value2, Action callback = null);
			[Cache(CacheType.InMemory, ExcludeParameterIndexes = new[] { 2 })]
			Task<int> ComputeSumWithCallbackTaskAsync(int value1, int value2, Task callbackTask);
		}

		public class MemoryCacheTestClass : IMemoryCacheTestClass
		{			
			public virtual int ComputeSumWithCallback(int value1, int value2, Action callback = null)
			{
				var result = value1 + value2;
				callback?.Invoke();
				return result;
			}
			
			public virtual async Task<int> ComputeSumWithCallbackTaskAsync(int value1, int value2, Task callbackTask)
			{
				var result = ComputeSumWithCallback(value1, value2, null);
				if (callbackTask != null)
				{
					if (callbackTask.Status == TaskStatus.Created)
					{
						callbackTask.Start();
					}
					await callbackTask;
				}
				return result;
			}
		}

		public interface IDistibutedCacheTestClass
		{
			[Cache(CacheType.Distributed, ExcludeParameterTypes = new[] { typeof(Action) })]
			int ComputeSumWithCallback(int value1, int value2, Action callback = null);
			[Cache(CacheType.Distributed, ExcludeParameterIndexes = new[] { 2 })]
			Task<int> ComputeSumWithCallbackTaskAsync(int value1, int value2, Task callbackTask);
		}

		public class DistibutedCacheTestClass : IDistibutedCacheTestClass
		{			
			public virtual int ComputeSumWithCallback(int value1, int value2, Action callback = null)
			{
				var result = value1 + value2;
				callback?.Invoke();
				return result;
			}
			
			public virtual async Task<int> ComputeSumWithCallbackTaskAsync(int value1, int value2, Task callbackTask)
			{
				var result = ComputeSumWithCallback(value1, value2, null);
				if (callbackTask != null)
				{
					if (callbackTask.Status == TaskStatus.Created)
					{
						callbackTask.Start();
					}
					await callbackTask;
				}
				return result;
			}
		}

		public interface IDummyTestClass
		{
			[Cache(CacheType.InMemory, SlidingExpiration = "00:00:00.1000000")]
			Task DummyMethodAsync(int arg1, int arg2);
		}

		public class DummyTestClass : IDummyTestClass
		{			
			public virtual async Task DummyMethodAsync(int arg1, int arg2)
			{
				await Task.Yield();
			}
		}

		[Test]
		public void MemoryCache_CanCacheSyncMethodCallWithConcurrentCalls()
		{
			using (var scope = this.Container.BeginLifetimeScope())
			{
				var testObj = new MemoryCacheTestClass();

				var factory = scope.Resolve<ICachingProxyFactory<IMemoryCacheTestClass>>();

				var options = new MemoryCacheEntryOptions();

				options.SetSlidingExpiration(TimeSpan.FromMilliseconds(10000));

				factory.ConfigureMethod<Func<int, int, Action, int>>(x => x.ComputeSumWithCallback, options, new InvocationContextConfig()
				{
					// ExcludeParameterTypes = new[] { typeof(Action) }
				});

				var proxy = factory.Create(testObj);

				var i = 0;

				var action = new Action(() =>
				{
					Task.Delay(1000).Wait();
					++i;
				});

				var syncRoot = new object();
				// SynchronizedCollection<int>
				// BlockingCollection<int>
				// ConcurrentBag<int>
				var results = new List<int>();
				var result1 = proxy.ComputeSumWithCallback(1, 2, action);
				Parallel.For(0, 10000, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, j =>
				{
					lock (syncRoot)
					{
						results.Add(proxy.ComputeSumWithCallback(1, 2, action));
					}
				});

				Assert.IsTrue(results.All(r => r == result1));

				Assert.AreEqual(1, i);
			}
		}

		[Test]
		public void MemoryCache_CanCacheSyncMethodCall()
		{
			using (var scope = this.Container.BeginLifetimeScope())
			{
				var testObj = new MemoryCacheTestClass();

				var factory = scope.Resolve<ICachingProxyFactory<IMemoryCacheTestClass>>();

				var options = new MemoryCacheEntryOptions();

				options.SetSlidingExpiration(TimeSpan.FromMilliseconds(500));

				factory.ConfigureMethod<Func<int, int, Action, int>>(x => x.ComputeSumWithCallback, options, new InvocationContextConfig());

				var proxy = factory.Create(testObj);

				var i = 0;

				var action = new Action(() =>
				{
					Task.Delay(100).Wait();
					++i;
				});

				var result1 = proxy.ComputeSumWithCallback(1, 2, action);

				var result2 = proxy.ComputeSumWithCallback(1, 2, action);

				Assert.AreEqual(result2, result1);

				Assert.AreEqual(1, i);

				Task.Delay(1000).Wait();

				proxy.ComputeSumWithCallback(1, 2, action);

				Assert.AreEqual(2, i);
			}
		}

		[Test]
		public void DistributedCache_CanCacheSyncMethodCall()
		{
			using (Lock())
			using (var scope = Container.BeginLifetimeScope())
			{
				WaitUntilFileIsLocked(DistributedCachePath).Wait();
				File.Delete(DistributedCachePath);

				var testObj = new DistibutedCacheTestClass();

				var factory = scope.Resolve<ICachingProxyFactory<IDistibutedCacheTestClass>>();

				var options = new DistributedCacheEntryOptions();

				options.SetSlidingExpiration(TimeSpan.FromSeconds(1));

				factory.ConfigureMethod<Func<int, int, Action, int>>(x => x.ComputeSumWithCallback, options, new InvocationContextConfig());

				var proxy = factory.Create(testObj);

				var i = 0;

				var action = new Action(() =>
				{
					Task.Delay(100).Wait();
					++i;
				});

				var result1 = proxy.ComputeSumWithCallback(1, 2, action);

				var result2 = proxy.ComputeSumWithCallback(1, 2, action);

				Assert.AreEqual(result2, result1);

				Assert.AreEqual(1, i);

				Task.Delay(1000).Wait();

				proxy.ComputeSumWithCallback(1, 2, action);

				Assert.AreEqual(2, i);
			}
		}

		[Test]
		public void MemoryCache_CanCacheWithSlidingExpiration()
		{
			using (var scope = this.Container.BeginLifetimeScope())
			{
				var testObj = new MemoryCacheTestClass();

				var factory = scope.Resolve<ICachingProxyFactory<IMemoryCacheTestClass>>();

				var options = new MemoryCacheEntryOptions();

				var isEvicted = false;

				options.RegisterPostEvictionCallback((key, value, reason, state) =>
				{
					isEvicted = true;
				});

				// options.SetSlidingExpiration(TimeSpan.FromMilliseconds(100)); // attribute used

				factory.ConfigureMethod<Func<int, int, Action, int>>(x => x.ComputeSumWithCallback, options, new InvocationContextConfig());

				var proxy = factory.Create(testObj);

				var i = 0;

				var action = new Action(() =>
				{
					Task.Delay(50).Wait();
					++i;
				});

				proxy.ComputeSumWithCallback(1, 2, action);

				proxy.ComputeSumWithCallback(1, 2, action);

				Task.Delay(100).Wait();

				proxy.ComputeSumWithCallback(1, 2, action);

				Assert.AreEqual(2, i);

				Assert.IsTrue(isEvicted);
			}
		}

		[Test]
		public void DistributedCache_CanCacheWithSlidingExpiration()
		{
			using (Lock())
			using (var scope = this.Container.BeginLifetimeScope())
			{
				WaitUntilFileIsLocked(DistributedCachePath).Wait();
				File.Delete(DistributedCachePath);

				var testObj = new DistibutedCacheTestClass();

				var factory = scope.Resolve<ICachingProxyFactory<IDistibutedCacheTestClass>>();

				var options = new DistributedCacheEntryOptions();				

				options.SetSlidingExpiration(TimeSpan.FromSeconds(1)); // attribute used

				factory.ConfigureMethod<Func<int, int, Action, int>>(x => x.ComputeSumWithCallback, options, new InvocationContextConfig());

				var proxy = factory.Create(testObj);

				var i = 0;

				var action = new Action(() =>
				{
					Task.Delay(50).Wait();
					++i;
				});

				proxy.ComputeSumWithCallback(1, 2, action);

				proxy.ComputeSumWithCallback(1, 2, action);

				Task.Delay(1000).Wait();

				proxy.ComputeSumWithCallback(1, 2, action);

				Assert.AreEqual(2, i);
			}
		}

		[Test]
		public void MemoryCache_CanCacheWithAbsoluteExpiration()
		{
			using (var scope = this.Container.BeginLifetimeScope())
			{
				var testObj = new MemoryCacheTestClass();

				var factory = scope.Resolve<ICachingProxyFactory<IMemoryCacheTestClass>>();

				var options = new MemoryCacheEntryOptions();

				var isEvicted = false;

				options.RegisterPostEvictionCallback((key, value, reason, state) =>
				{
					isEvicted = true;
				});

				options.SetAbsoluteExpiration(DateTime.Now.AddMilliseconds(500));

				factory.ConfigureMethod<Func<int, int, Action, int>>(x => x.ComputeSumWithCallback, options, new InvocationContextConfig());

				var proxy = factory.Create(testObj);

				var i = 0;

				var action = new Action(() =>
				{
					Task.Delay(50).Wait();
					++i;
				});

				proxy.ComputeSumWithCallback(1, 2, action);

				proxy.ComputeSumWithCallback(1, 2, action);

				Task.Delay(500).Wait();

				proxy.ComputeSumWithCallback(1, 2, action);

				Assert.AreEqual(2, i);

				Assert.IsTrue(isEvicted);

				isEvicted = false;

				options = new MemoryCacheEntryOptions();

				options.RegisterPostEvictionCallback((key, value, reason, state) => { isEvicted = true; });

				options.SetAbsoluteExpiration(TimeSpan.FromMilliseconds(500));

				factory.ConfigureMethod<Func<int, int, Action, int>>(x => x.ComputeSumWithCallback, options, new InvocationContextConfig());

				proxy = factory.Create(testObj);

				i = 0;

				proxy.ComputeSumWithCallback(1, 2, action);

				proxy.ComputeSumWithCallback(1, 2, action);

				Task.Delay(500).Wait();

				proxy.ComputeSumWithCallback(1, 2, action);

				Assert.AreEqual(2, i);

				Assert.IsTrue(isEvicted);
			}
		}

		[Test]
		public void DistributedCache_CanCacheWithAbsoluteExpiration()
		{
			using (Lock())
			using (var scope = Container.BeginLifetimeScope())
			{
				WaitUntilFileIsLocked(DistributedCachePath).Wait();
				File.Delete(DistributedCachePath);

				var testObj = new DistibutedCacheTestClass();

				var factory = scope.Resolve<ICachingProxyFactory<IDistibutedCacheTestClass>>();

				var options = new DistributedCacheEntryOptions();

				options.SetAbsoluteExpiration(DateTime.Now.AddSeconds(1));

				factory.ConfigureMethod<Func<int, int, Action, int>>(x => x.ComputeSumWithCallback, options, new InvocationContextConfig());

				var proxy = factory.Create(testObj);

				var i = 0;

				var action = new Action(() =>
				{
					Task.Delay(50).Wait();
					++i;
				});

				proxy.ComputeSumWithCallback(1, 2, action);

				proxy.ComputeSumWithCallback(1, 2, action);

				Task.Delay(1000).Wait();

				proxy.ComputeSumWithCallback(1, 2, action);

				Assert.AreEqual(2, i);
			}
		}

		[Test]
		public async Task MemoryCache_CantCacheAsyncMethodCallIfMethodHasNoResult()
		{
			using (var scope = this.Container.BeginLifetimeScope())
			{
				var testObj = new DummyTestClass();

				var factory = scope.Resolve<ICachingProxyFactory<IDummyTestClass>>();

				var catched = false;

				var proxy = factory.Create(testObj);
				try
				{
					await proxy.DummyMethodAsync(1, 2);
				}
				catch (InvalidOperationException)
				{
					catched = true;
				}

				Assert.IsTrue(catched);
			}
		}

		[Test]
		public async Task MemoryCache_CanCacheAsyncMethodCall()
		{
			using (var scope = this.Container.BeginLifetimeScope())
			{
				var testObj = new MemoryCacheTestClass();

				var factory = scope.Resolve<ICachingProxyFactory<IMemoryCacheTestClass>>();

				var options = new MemoryCacheEntryOptions();

				options.SetSlidingExpiration(TimeSpan.FromMilliseconds(1000));

				factory.ConfigureMethod<Func<int, int, Task<int>, Task<int>>>(x => x.ComputeSumWithCallbackTaskAsync, options, new InvocationContextConfig());

				var proxy = factory.Create(testObj);

				var i = 0;

				var action = new Action(() =>
				{
					Task.Delay(100).Wait();
					++i;
				});

				var result1 = await proxy.ComputeSumWithCallbackTaskAsync(1, 2, new Task(action));

				var result2 = await proxy.ComputeSumWithCallbackTaskAsync(1, 2, new Task(action));

				Assert.AreEqual(result1, result2);

				Assert.AreEqual(1, i); // same as j

				Task.Delay(1000).Wait();

				await proxy.ComputeSumWithCallbackTaskAsync(1, 2, new Task(action));

				Assert.AreEqual(2, i);
			}
		}

		[Test]
		public async Task DistributedCache_CanCacheAsyncMethodCall()
		{
			using (Lock())
			using (var scope = Container.BeginLifetimeScope())
			{
				await WaitUntilFileIsLocked(DistributedCachePath);
				File.Delete(DistributedCachePath);

				var testObj = new DistibutedCacheTestClass();

				var factory = scope.Resolve<ICachingProxyFactory<IDistibutedCacheTestClass>>();

				var options = new DistributedCacheEntryOptions();

				options.SetSlidingExpiration(TimeSpan.FromSeconds(1));

				factory.ConfigureMethod<Func<int, int, Task<int>, Task<int>>>(x => x.ComputeSumWithCallbackTaskAsync, options, new InvocationContextConfig());

				var proxy = factory.Create(testObj);

				var i = 0;

				var action = new Action(() =>
				{
					Task.Delay(100).Wait();
					++i;
				});

				var result1 = await proxy.ComputeSumWithCallbackTaskAsync(1, 2, new Task(action));

				var result2 = await proxy.ComputeSumWithCallbackTaskAsync(1, 2, new Task(action));

				Assert.AreEqual(result1, result2);

				Assert.AreEqual(1, i); // same as j

				Task.Delay(1000).Wait();

				await proxy.ComputeSumWithCallbackTaskAsync(1, 2, new Task(action));

				Assert.AreEqual(2, i);
			}
		}

	}
}




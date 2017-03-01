using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Autofac;
using Autofac.Features.OwnedInstances;

namespace AutofacLifetimeTest
{
	public class TestResource : IDisposable
	{
		public static int _liveCount = 0;
		public static int _nonDisposedCount = 0;

		public TestResource()
		{
			Interlocked.Increment(ref _liveCount);
			Interlocked.Increment(ref _nonDisposedCount);
		}

		public void ResourceOperation()
		{
			if (_disposed) { throw new InvalidOperationException("Resource disposed!"); }
		}

		private bool _disposed = false;

		public void Dispose()
		{
			_disposed = true;
			Interlocked.Decrement(ref _nonDisposedCount);
		}

		~TestResource()
		{
			Interlocked.Decrement(ref _liveCount);
		}
	}

	public class TestService
	{
		private readonly Func<TestResource> _resourceFactory;
		private readonly StateMediator _stateMediator;

		public TestService(StateMediator stateMediator, Func<TestResource> resourceFactory)
		{
			_stateMediator = stateMediator;
			_resourceFactory = resourceFactory;
		}

		public void DoSomething()
		{
			using (var resource = _resourceFactory())
			{
				resource.ResourceOperation();
			}
		}
	}

	public class StateMediator
	{
		public string SomeSharedState { get; set; }
	}

	public class Program
	{
		private static void Main(string[] args)
		{
			var builder = new ContainerBuilder();
			builder.RegisterType<TestService>().SingleInstance();
			builder.RegisterType<TestResource>().InstancePerLifetimeScope();
			builder.Register<Func<TestResource>>(ctx =>
			{
				var cc = ctx.Resolve<IComponentContext>();
				return () => cc.Resolve<Owned<TestResource>>().Value;
			});

			builder.RegisterType<StateMediator>().SingleInstance();

			var container = builder.Build();
			var process = Process.GetCurrentProcess();

			using (var scope = container.BeginLifetimeScope())
			{
				long counter = 0;
				while (true)
				{
					using (var innerScope = scope.BeginLifetimeScope())
					{
						var service = innerScope.Resolve<TestService>();
						service.DoSomething();
					}
					counter++;
					if (counter % 100000 == 0)
					{
						process.Refresh();
						Console.WriteLine("Memory: {0:#,##.0}kB, Live: {1}, Undisposed: {2}", process.PrivateMemorySize64 / 1024.0, TestResource._liveCount, TestResource._nonDisposedCount);
					}
				}
			}
		}
	}
}
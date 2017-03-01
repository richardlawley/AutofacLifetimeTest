using System;
using System.Diagnostics;
using System.Linq;
using Autofac;
using Autofac.Features.OwnedInstances;

namespace AutofacLifetimeTest
{
	public class TestResource : IDisposable
	{
		public void Dispose()
		{
		}
	}

	public class TestService
	{
		private readonly Func<TestResource> _resourceFactory;
		public TestService(Func<TestResource> resourceFactory)
		//private readonly Func<Owned<TestResource>> _resourceFactory;
		//public TestService(Func<Owned<TestResource>> resourceFactory)
		{
			_resourceFactory = resourceFactory;
		}

		public void DoSomething()
		{
			using (var resource = _resourceFactory())
			{

			}
		}
	}

	public class Program
	{
		private static void Main(string[] args)
		{
			var builder = new ContainerBuilder();
			builder.RegisterType<TestService>().SingleInstance();
			builder.RegisterType<TestResource>();
			var container = builder.Build();

			var process = Process.GetCurrentProcess();

			using (var scope = container.BeginLifetimeScope())
			{
				var service = scope.Resolve<TestService>();
				long counter = 0;
				while (true)
				{
					service.DoSomething();
					counter++;
					if (counter % 100000 == 0)
					{
						process.Refresh();
						Console.WriteLine("Memory: {0:#,##.0}kB", process.PrivateMemorySize64 / 1024.0);
					}
				}
			}
		}
	}
}
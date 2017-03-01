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
			builder.RegisterType<TestService>().InstancePerLifetimeScope();
			builder.RegisterType<TestResource>().InstancePerLifetimeScope();
			builder.RegisterType<StateMediator>().SingleInstance();

			var container = builder.Build();
			var process   = Process.GetCurrentProcess();

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
						Console.WriteLine("Memory: {0:#,##.0}kB", process.PrivateMemorySize64 / 1024.0);
					}
				}
			}
		}
	}
}
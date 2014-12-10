﻿using NUnit.Framework;

namespace DryIoc.UnitTests
{
    [TestFixture]
    public class ConstructionTests
    {
        [Test]
        public void Can_use_static_method_for_service_creation()
        {
            var container = new Container();
            container.Register<SomeService>(rules: InjectionRules.With(
                r => FactoryMethod.Of(r.ImplementationType.GetDeclaredMethodOrNull("Create"))));

            var service = container.Resolve<SomeService>();

            Assert.That(service.Message, Is.EqualTo("static"));
        }

        [Test]
        public void Can_use_any_type_static_method_for_service_creation()
        {
            var container = new Container();
            container.Register<IService>(rules: typeof(ServiceFactory).GetDeclaredMethodOrNull("CreateService"));

            var service = container.Resolve<IService>();

            Assert.That(service.Message, Is.EqualTo("static"));
        }

        [Test]
        public void Can_use_any_type_static_method_for_service_creation_Refactoring_friendly()
        {
            var container = new Container();
            container.Register<IService>(rules: FactoryMethod.Of(() => ServiceFactory.CreateService()));

            var service = container.Resolve<IService>();

            Assert.That(service.Message, Is.EqualTo("static"));
        }

        [Test]
        public void Can_use_instance_method_for_service_creation()
        {
            var container = new Container();
            container.Register<ServiceFactory>();
            container.Register<IService>(rules: InjectionRules.With(r => FactoryMethod.Of(
                typeof(ServiceFactory).GetDeclaredMethodOrNull("Create"), r.Resolve<ServiceFactory>())));

            var service = container.Resolve<IService>();

            Assert.That(service.Message, Is.EqualTo("instance"));
        }

        [Test]
        public void Can_use_instance_method_with_resolved_parameter()
        {
            var container = new Container();
            container.Register<ServiceFactory>();
            container.RegisterInstance("parameter");
            container.Register<IService>(rules: InjectionRules.With(r => FactoryMethod.Of(
                typeof(ServiceFactory).GetDeclaredMethodOrNull("Create", typeof(string)), r.Resolve<ServiceFactory>())));

            var service = container.Resolve<IService>();

            Assert.That(service.Message, Is.EqualTo("parameter"));
        }

        [Test]
        public void Should_throw_if_instance_factory_unresolved()
        {
            var container = new Container();
            container.Register<SomeService>(rules: InjectionRules.With(r => FactoryMethod.Of(
                typeof(ServiceFactory).GetDeclaredMethodOrNull("Create"), r.Resolve<ServiceFactory>())));

            var ex = Assert.Throws<ContainerException>(() =>
                container.Resolve<SomeService>());

            Assert.That(ex.Message, Is.StringContaining("Unable to resolve"));
        }

        [Test]
        public void Should_throw_for_instance_method_without_factory()
        {
            var container = new Container();
            container.Register<IService>(rules: typeof(ServiceFactory).GetDeclaredMethodOrNull("Create"));

            var ex = Assert.Throws<ContainerException>(() => 
                container.Resolve<IService>());

            Assert.That(ex.Message, Is.StringContaining("Unable to use null factory object with factory method"));
        }

        [Test]
        public void Should_return_null_if_instance_factory_is_not_resolved_on_TryResolve()
        {
            var container = new Container();
            container.Register<IService>(rules: InjectionRules.With(r => FactoryMethod.Of(
                typeof(ServiceFactory).GetDeclaredMethodOrNull("Create"), r.Resolve<ServiceFactory>())));

            var service = container.Resolve<IService>(IfUnresolved.ReturnDefault);

            Assert.That(service, Is.Null);
        }

        [Test]
        public void What_if_factory_method_returned_incompatible_type()
        {
            var container = new Container();
            container.Register<SomeService>(rules: typeof(BadFactory).GetDeclaredMethodOrNull("Create"));

            var ex = Assert.Throws<ContainerException>(() =>
                container.Resolve<SomeService>());

            Assert.That(ex.Message, Is.StringContaining("SomeService is not assignable from factory method"));
        }

        #region CUT

        internal interface IService 
        {
            string Message { get; }
        }

        internal class SomeService : IService
        {
            public string Message { get; private set; }

            internal SomeService(string message)
            {
                Message = message;
            }

            public static SomeService Create()
            {
                return new SomeService("static");
            }
        }

        internal class ServiceFactory
        {
            public static IService CreateService()
            {
                return new SomeService("static");
            }

            public IService Create()
            {
                return new SomeService("instance");
            }

            public IService Create(string parameter)
            {
                return new SomeService(parameter);
            }
        }

        internal class BadFactory
        {
            public static string Create()
            {
                return "bad";
            }
        }

        internal class Generic<T>
        {
            public T X { get; private set; }

            public Generic(T x)
            {
                X = x;
            }
        }

        #endregion
    }
}
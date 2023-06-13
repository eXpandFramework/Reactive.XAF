using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using NUnit.Framework;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Tests {
    [SuppressMessage("ReSharper", "UseEventArgsEmptyField")]
    public class WhenEventTests {
        [Test]
        public async Task StaticEvent() {
            var observable = typeof(MyStaticClass).WhenEvent("MyStaticEvent");
            var eventHandled = false;
            var subscription = observable.Subscribe(_ => eventHandled = true);


            MyStaticClass.TriggerEvent();
            await Task.Delay(100);


            Assert.IsTrue(eventHandled);
            subscription.Dispose();
        }

        [Test]
        public void WhenEvent_ObjectSource_SingleEventName_ReturnsEventPattern() {
            var source = new SampleClass();
            var eventName = "Event1";
            var eventFired = false;

            var observable = source.WhenEvent(eventName);
            observable.Subscribe(_ => eventFired = true);

            source.InvokeEvent1(null);

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void WhenEvent_ObjectSource_MultipleEventNames_ReturnsCombinedEventPattern() {
            var source = new SampleClass();
            var eventNames = new[] { "Event1", "Event2" };
            var eventsFired = new List<EventArgs>();

            var observable = source.WhenEvent(eventNames);
            observable.Subscribe(pattern => eventsFired.Add((EventArgs)pattern.EventArgs));

            var eventArgs1 = new EventArgs();
            source.InvokeEvent1(eventArgs1);
            var eventArgs2 = new EventArgs();
            source.InvokeEvent2(eventArgs2);

            Assert.AreEqual(2, eventsFired.Count);
            Assert.Contains(eventArgs1, eventsFired);
            Assert.Contains(eventArgs2, eventsFired);
        }

        [Test]
        public void WhenEvent_TypeSource_SingleEventName_ReturnsEventPattern() {
            var source = typeof(SampleClass);
            var eventName = "StaticEvent1";
            var eventFired = false;

            var observable = source.WhenEvent(eventName);
            observable.Subscribe(_ => eventFired = true);

            SampleClass.InvokeStaticEvent1();

            Assert.IsTrue(eventFired);
        }


        [Test]
        public void WhenEvent_GenericTypeSource_SingleEventName_ReturnsEventPattern() {
            var source = new GenericSampleClass<string>();
            var eventName = "Event1";
            var eventFired = false;

            var observable = source.WhenEvent<string>(eventName);
            observable.Subscribe(_ => eventFired = true);

            source.InvokeEvent1("Event1 Args");

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void WhenEvent_GenericTypeSource_MultipleEventNames_ReturnsCombinedEventPattern() {
            var source = new GenericSampleClass<string>();
            var eventNames = new[] { "Event1", "Event2" };
            var eventsFired = new List<string>();

            var observable = source.WhenEvent<string>(eventNames);
            observable.Subscribe(pattern => eventsFired.Add(pattern));

            source.InvokeEvent1("Event1");
            source.InvokeEvent2("Event2");

            Assert.AreEqual(2, eventsFired.Count);
            Assert.Contains("Event1", eventsFired);
            Assert.Contains("Event2", eventsFired);
        }

        [Test]
        public void WhenEvent_ObjectSource_GenericEventArgs_SingleEventName_ReturnsEventPattern() {
            var source = new SampleClass();
            var eventName = "Event3";
            var eventFired = false;

            var observable = source.WhenEvent<CustomEventArgs>(eventName);
            observable.Subscribe(_ => eventFired = true);

            source.InvokeEvent3(new CustomEventArgs());

            Assert.IsTrue(eventFired);
        }




    }

    public class SampleClass {
        public event EventHandler Event1;
        public event EventHandler Event2;

        public static event EventHandler StaticEvent1;
        public static event EventHandler StaticEvent2;

        public event EventHandler<CustomEventArgs> Event3;
        public event EventHandler<CustomEventArgs> Event4;

        

        public void InvokeEvent1(EventArgs args) {
            Event1?.Invoke(this, args);
        }

        public void InvokeEvent2(EventArgs args) {
            Event2?.Invoke(this,args);
        }

        public static void InvokeStaticEvent1() {
            StaticEvent1?.Invoke(null, EventArgs.Empty);
        }

        public static void InvokeStaticEvent2() {
            StaticEvent2?.Invoke(null, EventArgs.Empty);
        }

        public void InvokeEvent3(CustomEventArgs args) {
            Event3?.Invoke(this, args);
        }

        public void InvokeEvent4(CustomEventArgs args) {
            Event4?.Invoke(this, args);
        }

        
    }

    public class GenericSampleClass<T> {
        public event EventHandler<T> Event1;
        public event EventHandler<T> Event2;

        public static event EventHandler<T> StaticEvent1;
        public static event EventHandler<T> StaticEvent2;

        public event EventHandler<CustomEventArgs> Event3;
        public event EventHandler<CustomEventArgs> Event4;

        public static event EventHandler<CustomEventArgs> StaticEvent3;
        public static event EventHandler<CustomEventArgs> StaticEvent4;

        public void InvokeEvent1(T args) {
            Event1?.Invoke(this, args);
        }

        public void InvokeEvent2(T args) {
            Event2?.Invoke(this, args);
        }

        public static void InvokeStaticEvent1(T args) {
            StaticEvent1?.Invoke(null, args);
        }

        public static void InvokeStaticEvent2(T args) {
            StaticEvent2?.Invoke(null, args);
        }

        public void InvokeEvent3(CustomEventArgs args) {
            Event3?.Invoke(this, args);
        }

        public void InvokeEvent4(CustomEventArgs args) {
            Event4?.Invoke(this, args);
        }

        public static void InvokeStaticEvent3(CustomEventArgs args) {
            StaticEvent3?.Invoke(null, args);
        }

        public static void InvokeStaticEvent4(CustomEventArgs args) {
            StaticEvent4?.Invoke(null, args);
        }
    }


    public class CustomEventArgs : EventArgs {
        // Add properties and logic as needed
    }

    internal static class MyStaticClass {
        public static event EventHandler MyStaticEvent;

        public static void TriggerEvent() {
            MyStaticEvent?.Invoke(null, EventArgs.Empty);
        }
    }
}
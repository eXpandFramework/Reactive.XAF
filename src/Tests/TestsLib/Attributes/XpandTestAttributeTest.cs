using System;
using System.Threading;
using NUnit.Framework;
using Shouldly;

namespace Xpand.TestsLib.Attributes{
    public class XpandTestAttributeTest{
        private static int _retryWhenTestcaseFailCount;
        private static int _retryWhenTestFailCount;
        private static int _failAndRetryWhenTimeoutPassed;
        private static bool _failAndRetryWhenTimeoutPassedTimeout=true;

        [XpandTest()]
        [Test]
        public void Retry_When_Test_Fail(){
            _retryWhenTestFailCount++;
            if (_retryWhenTestFailCount < 2){
                throw new Exception(_retryWhenTestFailCount.ToString());
            }
        }


        [XpandTest]
        [Test]
        [Apartment(ApartmentState.STA)]
        public void Donot_Change_AppartmentState(){
            var currentThreadApartmentState = Thread.CurrentThread.GetApartmentState();
            currentThreadApartmentState.ShouldBe(ApartmentState.STA);
        }

        [XpandTest(1000)]
        [Test]
        
        public void Fail_and_retry_When_Timeout_Passed(){
            _failAndRetryWhenTimeoutPassed++;

            if (_failAndRetryWhenTimeoutPassed == 1){
                Thread.Sleep(5000);
                _failAndRetryWhenTimeoutPassedTimeout = false;
            }
            _failAndRetryWhenTimeoutPassedTimeout.ShouldBe(true);
            _failAndRetryWhenTimeoutPassed.ShouldBe(2);
        }


        [XpandTest()]
        [TestCase(1)]
        [TestCase(2)]
        public void Retry_When_TestCase_Fail(int a){
            _retryWhenTestcaseFailCount++;
            if (_retryWhenTestcaseFailCount < 2 * a){
                throw new Exception(_retryWhenTestcaseFailCount.ToString());
            }
        }
    }
}
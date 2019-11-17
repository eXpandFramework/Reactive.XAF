using System;
using System.Threading;
using NUnit.Framework;
using Shouldly;

namespace Xpand.TestsLib.Attributes{
    public class XpandTestAttributeTest{

        private static int _retryWhenTestcaseFailCount;
        private static int _retryWhenTestFailCount;
        private static int _timeoutTest;

        [XpandTest()]
        [Test]
        public void Retry_When_Test_Fail(){
            _retryWhenTestFailCount++;
            if (_retryWhenTestFailCount < 2){
                throw new Exception(_retryWhenTestFailCount.ToString());    
            }
        }
        
        
        [XpandTest()]
        [Test]
        public void Fail_When_Timeout_Passed(){
            _timeoutTest++;
            if (_timeoutTest==1){
                Thread.Sleep(1500);
            }
            _timeoutTest.ShouldBe(2);
        }


        [XpandTest()]
        [TestCase(1)]
        [TestCase(2)]
        public void Retry_When_TestCase_Fail(int a){
            _retryWhenTestcaseFailCount++;
            if (_retryWhenTestcaseFailCount < 2*a){
                throw new Exception(_retryWhenTestcaseFailCount.ToString());    
            }
        }

    }
}
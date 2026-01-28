using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void TestMethod1()//нормальныйслучай
    {
        int limit = 10;
        List<int> res = new List<int> { 2, 3, 5, 7 };
        SieveOfEratosthenesService sieve = new SieveOfEratosthenesService();
        
        List<int> array = sieve.FindPrimes(limit);
        
        CollectionAssert.AreEqual(res, array);
    }
    
    [TestMethod]
    public void TestMethod2()//лимитмаленький
    {
        int limit = 2;
        List<int> res = new List<int> { 2 };
        SieveOfEratosthenesService sieve = new SieveOfEratosthenesService();
        
        List<int> array = sieve.FindPrimes(limit);
        
        CollectionAssert.AreEqual(res, array);
    }
    
    [TestMethod]
    public void TestMethod3()//лимит1
    {
        int limit = 1;
        List<int> res = new List<int> { };
        SieveOfEratosthenesService sieve = new SieveOfEratosthenesService();
        
        List<int> array = sieve.FindPrimes(limit);
        
        CollectionAssert.AreEqual(res, array);
    }
    
    [TestMethod]
    public void TestMethod4()//лимит0
    {
        int limit = 0;
        List<int> res = new List<int> { };
        SieveOfEratosthenesService sieve = new SieveOfEratosthenesService();
        
        List<int> array = sieve.FindPrimes(limit);
        
        CollectionAssert.AreEqual(res, array);
    }
    
    [TestMethod]
    public void TestMethod5()//лимит30
    {
        int limit = 30;
        List<int> res = new List<int> { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29 };
        SieveOfEratosthenesService sieve = new SieveOfEratosthenesService();
        
        List<int> array = sieve.FindPrimes(limit);
        
        CollectionAssert.AreEqual(res, array);
    }
    
    [TestMethod]
    public void TestMethod6()//проверяемсколькочиселдо100
    {
        int limit = 100;
        SieveOfEratosthenesService sieve = new SieveOfEratosthenesService();
        
        List<int> array = sieve.FindPrimes(limit);
        
        //до100должнобыть25простыхчисел
        Assert.HasCount(25, array);
    }
}
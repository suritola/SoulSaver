using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// 자동 플레이 측정 진입점 (tools/autoplay/run.ps1 이 프로젝트 복사본에만 넣음)
// 봇 본체(AutoplayRunner · AutoplayBot)는 복사본의 Assets/Scripts/Autoplay 에 들어가 게임 코드와 같은 어셈블리로 컴파일됨
// 테스트 어셈블리는 Assembly-CSharp 를 참조할 수 없어서 리플렉션으로 한 번만 부름
public class AutoplayTest
{
    [UnityTest, Timeout(int.MaxValue)]
    public IEnumerator Run()
    {
        LogAssert.ignoreFailingMessages = true;
        Type t = Type.GetType("AutoplayRunner, Assembly-CSharp");
        Assert.IsNotNull(t, "AutoplayRunner 를 찾지 못함 (복사본에 봇 코드가 없음)");
        var routine = (IEnumerator)t.GetMethod("Run", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
        yield return routine;
    }
}

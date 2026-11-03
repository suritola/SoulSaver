using System.Text;
using TMPro;

// 2.0.1~ 일본어 가타카나 낱말 줄바꿈: TMP 는 일본어를 글자마다 끊어 「キャラク/ター」 「1日1プ/レイ」처럼 낱말 가운데서 줄이 바뀜
//   → 화면에 그릴 때만 가타카나 묶음(2 ~ 10자, 「ー」 포함)을 <nobr> 로 감쌈. text 원문은 그대로라 검색 · 복사 · 비교에 영향 없음
//   UIKit.Text 가 만든 글에 붙음 (일본어일 때만 바꿈 · 가타카나가 없으면 같은 문자열을 돌려줌)
//   shortcut: 한자 · 히라가나 낱말과 중국어는 낱말 사전이 없어 그대로 (TMP 금칙 문자 목록만 적용), 필요하면 형태소 단위 표로 확장
public sealed class JaWordWrap : ITextPreprocessor
{
    public static readonly JaWordWrap Instance = new JaWordWrap();

    static bool Kata(char c) => c >= 'ァ' && c <= 'ヺ' || c == 'ー';      // ァ ~ ヺ · ー (・ 는 빼서 낱말 사이로 둠)

    public string PreprocessText(string text)
    {
        if (Loc.Current != Loc.Lang.Japanese || string.IsNullOrEmpty(text)) return text;
        StringBuilder b = null;
        int i = 0, n = text.Length;
        bool inTag = false;
        while (i < n)
        {
            char c = text[i];
            if (c == '<') inTag = true;
            else if (c == '>') inTag = false;
            if (!inTag && Kata(c))
            {
                int j = i;
                while (j < n && Kata(text[j])) j++;
                int len = j - i;
                if (len >= 2 && len <= 10)
                {
                    b ??= new StringBuilder(text, 0, i, n + 32);
                    b.Append("<nobr>").Append(text, i, len).Append("</nobr>");
                    i = j;
                    continue;
                }
                b?.Append(text, i, len);
                i = j;
                continue;
            }
            b?.Append(c);
            i++;
        }
        return b != null ? b.ToString() : text;
    }
}

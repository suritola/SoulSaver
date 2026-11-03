using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 2.0.0~ 「역류 준비」 창 (modes.md 2-3): 영웅 · 갈래 · 1차 무기만 고르고 나머지 몸은 고정 시드로 자동 (LevelShop.AutoFill)
//   특별 모드 창의 역류 카드 [고르기] 가 ModeHubUI.RefluxOpener 로 부름 → [역류 시작] → RefluxRun.Begin(영웅, 갈래, 1차 무기)
//   메뉴에서는 판 밖이라 자동 채우기를 미리 돌려 볼 수 없어(카드 · 진화 아이콘 미리 보기는 생략) 갈래 한 줄 설명과 「완성된 몸」 요약만 보여 줌
public static class RefluxPrepUI
{
    static GameObject open;
    static Color Gold => UiRules.Legacy ? new Color(0.96f, 0.83f, 0.47f) : UiTokens.Text1;
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Dim = new Color(0.66f, 0.61f, 0.53f);
    static readonly Color Picked = new Color(1f, 0.9f, 0.55f);

    public static void Open(Transform root)
    {
        if (open != null || !RefluxRun.Unlocked) return;
        RectTransform win = UIKit.Modal(root, "RefluxPrep", new Vector2(1240f, 820f), out open);
        UIKit.EscCancel(open, () => { Close(); ModeHubUI.Open(root); });
        open.AddComponent<Prep>().Build(win, root);
    }

    public static void Close()
    {
        if (open != null) Object.Destroy(open);
        open = null;
    }

    // 1차 무기 형태 번호 목록 (거너 = 진화 1차 셋 · 그 밖 = 캐릭터 전용 무기 셋) — SpecialAbilities.NextEvolutionOptions(1차) 와 같은 순서
    public static List<string> FormNames(CharacterId who)
    {
        List<string> names = new List<string>();
        if (who == CharacterId.Gunner)
            foreach (int id in SpecialAbilities.Tier1Options) names.Add(SpecialAbilities.GunAugName(id, false));
        else
            foreach (int id in CharacterData.Def(who).pool)
                if (SpecialAbilities.IsKitWeapon(id)) names.Add(CharacterKit.FormAugName(id));
        return names;
    }

    // 2.0.1~ 1차 무기 형태 번호 (FormNames 와 같은 순서 · SpecialAbilities.RefluxFormIds 와 같은 목록)
    static List<int> FormIds(CharacterId who)
    {
        List<int> ids = new List<int>();
        if (who == CharacterId.Gunner) ids.AddRange(SpecialAbilities.Tier1Options);
        else
            foreach (int id in CharacterData.Def(who).pool)
                if (SpecialAbilities.IsKitWeapon(id)) ids.Add(id);
        return ids;
    }

    // 2.0.1~ 이 갈래에서 못 쓰는 1차 무기 (SpecialAbilities.DropDeadEvolutions 와 같은 규칙): 지도 제작자 「일필」은 도형을 닫지 않아 지구본 효과가 0
    //   자동 채우기는 외길이라 교차 · 행군이 없음 → 일필이면 늘 막힘 (예전엔 준비 화면에서 고른 지구본이 판에서 말없이 다른 무기로 바뀜)
    static bool FormBlocked(CharacterId who, int path, int formId) => who == CharacterId.Cartographer && path == CartographerPaths.Stroke && formId == SpecialAbilities.KitGlobe;

    class Prep : MonoBehaviour
    {
        Transform root;
        int hero, path, form;
        readonly Button[] cells = new Button[16];
        readonly Image[] cellPortrait = new Image[16];
        readonly TMP_Text[] cellName = new TMP_Text[16];
        readonly Button[] pathBtn = new Button[Paths.Count];
        readonly Button[] formBtn = new Button[3];
        TMP_Text pathInfo, bodyLine, recordLine, heroTitle;
        Button go;

        public void Build(RectTransform win, Transform rootTf)
        {
            root = rootTf;
            RefluxRun.LoadLast(out CharacterId h, out path, out form);
            hero = (int)h;
            TMP_Text title = UIKit.Text(win, "역류", 46f, Gold, new Vector2(0f, 355f), new Vector2(700f, 64f));
            title.fontSizeMin = 24f;
            UIKit.MakeButton(win, "닫기", new Vector2(510f, 355f), new Vector2(150f, 58f), () => { Close(); ModeHubUI.Open(root); }, 22f);
            heroTitle = UIKit.Text(win, "", 26f, Parch, new Vector2(0f, 300f), new Vector2(900f, 36f));

            // 영웅 격자 (가진 영웅만 누를 수 있음 · 5칸 × 3줄)
            int total = CharacterData.All.Length;
            for (int i = 0; i < total && i < cells.Length; i++)
            {
                int who = i;
                int col = i % 5, row = i / 5;
                Vector2 pos = new Vector2(-500f + 100f * col, 215f - 98f * row);
                Button b = UIKit.MakeButton(win, "", pos, new Vector2(94f, 90f), () => PickHero(who), 18f);
                TMP_Text bl = b.GetComponentInChildren<TMP_Text>(true);
                if (bl != null) bl.gameObject.SetActive(false);
                RectTransform pr = UIKit.Rect("Portrait", b.transform, new Vector2(0f, 10f), new Vector2(58f, 58f));
                cellPortrait[i] = pr.gameObject.AddComponent<Image>();
                cellPortrait[i].preserveAspect = true;
                cellPortrait[i].raycastTarget = false;
                cellName[i] = UIKit.Text(b.transform, "", 14f, Parch, new Vector2(0f, -34f), new Vector2(90f, 20f));
                cellName[i].fontSizeMin = 9f;
                b.interactable = CharacterData.IsUnlocked((CharacterId)i);
                b.navigation = new Navigation { mode = Navigation.Mode.Automatic };
                if (!b.interactable)
                {
                    ColorBlock cb = b.colors;
                    cb.disabledColor = new Color(0.4f, 0.4f, 0.45f, 0.9f);
                    b.colors = cb;
                }
                cells[i] = b;
            }

            // 오른쪽: 갈래 5 · 1차 무기 3
            UIKit.Text(win, "reflux.prep.path", 24f, Gold, new Vector2(270f, 255f), new Vector2(560f, 32f));
            for (int p = 0; p < pathBtn.Length; p++)
            {
                int pi = p;
                pathBtn[p] = UIKit.MakeButton(win, "", new Vector2(30f + 120f * p, 205f), new Vector2(114f, 62f), () => { path = pi; UISound.Click(); Refresh(); }, 18f);
            }
            pathInfo = UIKit.Text(win, "", 20f, Parch, new Vector2(270f, 110f), new Vector2(560f, 100f), TextAlignmentOptions.Top);
            pathInfo.enableAutoSizing = true;
            pathInfo.fontSizeMin = 13f;
            pathInfo.fontSizeMax = 20f;
            UIKit.Text(win, "reflux.prep.form", 24f, Gold, new Vector2(270f, 30f), new Vector2(560f, 32f));
            for (int f = 0; f < formBtn.Length; f++)
            {
                int fi = f;
                formBtn[f] = UIKit.MakeButton(win, "", new Vector2(90f + 190f * f, -25f), new Vector2(180f, 62f), () => { form = fi; UISound.Click(); Refresh(); }, 18f);
            }

            bodyLine = UIKit.Text(win, "", 22f, Parch, new Vector2(0f, -160f), new Vector2(1100f, 130f), TextAlignmentOptions.Top);
            bodyLine.enableAutoSizing = true;          // 2.0.1~ 남은 영혼 · 막힌 무기 줄이 더해져 넘치지 않게
            bodyLine.fontSizeMax = 22f;
            bodyLine.fontSizeMin = 14f;
            recordLine = UIKit.Text(win, "", 24f, Parch, new Vector2(0f, -250f), new Vector2(900f, 34f));
            go = UIKit.MakeButton(win, "reflux.prep.go", new Vector2(0f, -325f), new Vector2(380f, 80f), Depart, 32f);
            UIKit.Text(win, "reflux.prep.nosave", 20f, Dim, new Vector2(0f, -385f), new Vector2(1000f, 28f));
            Refresh();
            StartCoroutine(Focus());
        }

        IEnumerator Focus()
        {
            yield return null;
            if (hero >= 0 && hero < cells.Length && cells[hero] != null) GamePad.WarpTo((RectTransform)cells[hero].transform);
        }

        void PickHero(int who)
        {
            if (!CharacterData.IsUnlocked((CharacterId)who)) return;
            if (hero != who) { hero = who; form = 0; path = 0; }      // 2.0.1~ 갈래 번호도 처음으로 (영웅마다 갈래가 달라 번호만 남으면 엉뚱한 갈래가 골라져 있었음)
            UISound.Click();
            Refresh();
        }

        void Depart()
        {
            CharacterId who = (CharacterId)hero;
            if (!CharacterData.IsUnlocked(who)) return;
            Close();
            RefluxRun.Begin(who, path, form);
        }

        static void Tint(Button b, bool on)
        {
            Image img = b.GetComponent<Image>();
            if (img != null) img.color = on ? Picked : Color.white;
        }

        void Refresh()
        {
            CharacterId who = (CharacterId)hero;
            for (int i = 0; i < CharacterData.All.Length && i < cells.Length; i++)
            {
                bool known = CharacterData.IsUnlocked((CharacterId)i);
                cellPortrait[i].sprite = CharacterUI.MenuPortrait((CharacterId)i);
                cellPortrait[i].color = known ? Color.white : Color.black;
                cellName[i].text = known ? Loc.T(CharacterData.Def((CharacterId)i).name) : "???";
                Tint(cells[i], i == hero);
            }
            heroTitle.text = Loc.T(CharacterData.Def(who).name);
            for (int p = 0; p < pathBtn.Length; p++)
            {
                PathDef d = Paths.DefOf(who, p);
                TMP_Text l = pathBtn[p].GetComponentInChildren<TMP_Text>(true);
                if (l != null) { l.text = d != null ? Loc.T(d.name) : "-"; l.enableAutoSizing = true; l.fontSizeMin = 10f; }
                pathBtn[p].interactable = d != null;
                Tint(pathBtn[p], p == path);
            }
            PathDef pd = Paths.DefOf(who, path);
            pathInfo.text = pd == null ? "" : Loc.T(pd.tagline) + "\n<color=#A89C86>" + Loc.T("reflux.prep.strong") + " " + Loc.T(pd.strong) + "  ·  " + Loc.T("reflux.prep.weak") + " " + Loc.T(pd.weak) + "</color>";
            List<string> forms = FormNames(who);
            List<int> formIds = FormIds(who);
            bool Blocked(int f) => f < formIds.Count && FormBlocked(who, path, formIds[f]);
            if (form >= forms.Count) form = 0;
            bool anyBlocked = false;
            if (Blocked(form)) for (int f = 0; f < forms.Count; f++) if (!Blocked(f)) { form = f; break; }
            for (int f = 0; f < formBtn.Length; f++)
            {
                TMP_Text l = formBtn[f].GetComponentInChildren<TMP_Text>(true);
                bool has = f < forms.Count, blocked = Blocked(f);
                anyBlocked |= blocked;
                if (l != null) { l.text = has ? Loc.T(forms[f]) : "-"; l.enableAutoSizing = true; l.fontSizeMin = 10f; }
                formBtn[f].interactable = has && !blocked;
                Tint(formBtn[f], f == form);
            }
            bodyLine.text = Loc.T("reflux.prep.body") + "\n<color=#A89C86>" + Loc.T("reflux.prep.spirit") + "</color>"
                          + (anyBlocked ? "\n<color=#E89A7A>" + Loc.T("reflux.prep.formblocked") + "</color>" : "");
            float mine = RefluxRun.BestOf(who), all = RefluxRun.Best;
            recordLine.text = Loc.T("relay.hub.best") + "  <color=#F5D478>" + (all > 0f ? EndlessMode.Clock(all) : "-") + "</color>"
                            + (mine > 0f ? "   <size=80%><color=#A89C86>(" + Loc.T("reflux.prep.thishero") + " " + EndlessMode.Clock(mine) + ")</color></size>" : "");     // 2.0.1~ 이 영웅 기록이 없으면 줄이지 않고 뺌
            go.interactable = CharacterData.IsUnlocked(who);
        }
    }
}

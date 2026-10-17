# 판 기록 합쳐 읽기 (1.2~, 설계서 3-10)
# balance_log.csv 와 머리줄이 바뀔 때 따로 남긴 balance_log_*.csv(옛 형식)를 칸 이름으로 맞춰 한 표로 합친다.
#   옛 파일에 없는 칸(retry_kind · menu_gap_secs · milestones … )은 빈 칸, 맨 앞에 source(파일 이름) 칸
#   1.2 부터는 끝에 칸만 더해지면 게임이 머리줄만 바꿔 한 파일로 이어 쓰므로(RunLog.UpgradeHeader) 옛 줄은 뒤 칸이 비어 있음 → 그대로 빈 칸
# 쓰는 법:
#   python tools/balance/merge_logs.py                       (기본: %USERPROFILE%/AppData/LocalLow/박민근/Soul Saver)
#   python tools/balance/merge_logs.py <기록 폴더> [출력.csv]   (출력 기본: 기록 폴더/balance_log_merged.csv)
import csv
import glob
import os
import sys


def main():
    folder = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.expanduser("~"), "AppData", "LocalLow", "박민근", "Soul Saver")
    out = sys.argv[2] if len(sys.argv) > 2 else os.path.join(folder, "balance_log_merged.csv")
    files = sorted(glob.glob(os.path.join(folder, "balance_log_*.csv")))
    files = [f for f in files if os.path.basename(f) != os.path.basename(out)]
    main_log = os.path.join(folder, "balance_log.csv")
    if os.path.exists(main_log):
        files.append(main_log)
    if not files:
        print("기록 파일이 없습니다: " + folder)
        return 1

    columns, rows = [], []
    for path in files:
        with open(path, encoding="utf-8-sig", newline="") as f:
            reader = csv.reader(f)
            header = next(reader, None)
            if not header:
                continue
            for name in header:
                if name not in columns:
                    columns.append(name)
            for line in reader:
                if not line:
                    continue
                row = {"source": os.path.basename(path)}
                for i, name in enumerate(header):
                    row[name] = line[i] if i < len(line) else ""
                rows.append(row)

    with open(out, "w", encoding="utf-8-sig", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=["source"] + columns, restval="")
        writer.writeheader()
        writer.writerows(rows)
    print("{0}개 파일 · {1}판 → {2}".format(len(files), len(rows), out))
    return 0


if __name__ == "__main__":
    sys.exit(main())

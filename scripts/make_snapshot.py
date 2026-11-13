import sqlite3
import json
import os
import shutil

repo_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
src = os.path.join(repo_root, "src", "Presentation", "Web", "dotnet_tutorials.db")
dst_dir = os.path.join(repo_root, "data", "snapshots")
dst = os.path.join(dst_dir, "dotnet_tutorials_freeze.db")

os.makedirs(dst_dir, exist_ok=True)
if os.path.exists(src):
    shutil.copyfile(src, dst)
    print(f"Copied {src} to {dst}")
else:
    print(f"Warning: {src} does not exist!")

conn = sqlite3.connect(dst)
cur = conn.cursor()
cur.execute("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;")
tables = [r[0] for r in cur.fetchall()]

summary = {
    "database": "dotnet_tutorials_freeze.db",
    "freeze_date": "2026-11-14",
    "architecture_version": ".NET 10.0 / C# 14 (Phase 28 Freeze)",
    "total_tables": len(tables),
    "tables": {}
}

for t in tables:
    count = cur.execute(f'SELECT count(*) FROM "{t}"').fetchone()[0]
    summary["tables"][t] = count

summary_path = os.path.join(dst_dir, "database_summary.json")
with open(summary_path, "w", encoding="utf-8") as f:
    json.dump(summary, f, indent=2)

print(f"Snapshot summary written to {summary_path}. Total tables: {len(tables)}")
for k, v in list(summary["tables"].items())[:15]:
    print(f"  - {k}: {v}")

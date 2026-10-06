"""Writes a Markdown coverage table from Cobertura reports (stdlib only)."""
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict

totals = defaultdict(lambda: [0, 0])  # package -> [lines covered, lines valid]
for path in sys.argv[1:]:
    for package in ET.parse(path).iter("package"):
        lines = list(package.iter("line"))
        totals[package.get("name")][0] += sum(1 for line in lines if int(line.get("hits", "0")) > 0)
        totals[package.get("name")][1] += len(lines)

def percent(covered, valid):
    return f"{100 * covered / valid:.1f}%" if valid else "n/a"

print("## Code coverage\n")
if not totals:
    print("No production code covered yet.")
    sys.exit()
print("| Module | Lines | Coverage |\n|---|---:|---:|")
for name, (covered, valid) in sorted(totals.items()):
    print(f"| {name} | {covered}/{valid} | {percent(covered, valid)} |")
covered = sum(c for c, _ in totals.values())
valid = sum(v for _, v in totals.values())
print(f"| **Total** | {covered}/{valid} | **{percent(covered, valid)}** |")

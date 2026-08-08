import fs from "node:fs/promises";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const xmlPath = "E:/ZyperWin-next/ZyperWin++4.1/Release/Bin/ZyperData.xml";
const outputDir = "E:/ZyperWin-next/outputs/019fc282-ee76-74b2-8f07-f9ca61ab35a2";
const outputPath = `${outputDir}/ZyperWin++功能分类总览.xlsx`;
const categoryNames = {
  explorer: "外观/资源管理器",
  xingneng: "性能优化设置",
  safe: "安全设置",
  edge: "Edge优化设置",
  system: "系统设置",
  update: "更新设置",
  yinsi: "隐私设置",
};

const xml = await fs.readFile(xmlPath, "utf8");
const categories = [];
for (const match of xml.matchAll(/<Configuration\s+category="([^"]+)">([\s\S]*?)<\/Configuration>/g)) {
  categories.push([
    categoryNames[match[1]] ?? match[1],
    [...match[2].matchAll(/<Item\s+name="[^"]+"/g)].length,
  ]);
}

const workbook = Workbook.create();
const sheet = workbook.worksheets.add("分类总览");
sheet.showGridLines = false;

sheet.getRange("A1:B1").merge();
sheet.getRange("A1").values = [["ZyperWin++ 当前功能分类总览"]];
sheet.getRange("A1:B1").format = {
  fill: "#1F4E78",
  font: { bold: true, color: "#FFFFFF", size: 16 },
  horizontalAlignment: "center",
  verticalAlignment: "center",
};
sheet.getRange("A1:B1").format.rowHeight = 34;

sheet.getRange("A2:B2").merge();
sheet.getRange("A2").values = [["来源：当前运行版配置　统计日期：2026-08-02"]];
sheet.getRange("A2:B2").format = {
  fill: "#D9EAF7",
  font: { color: "#405466", size: 10 },
  horizontalAlignment: "left",
  verticalAlignment: "center",
};
sheet.getRange("A2:B2").format.rowHeight = 24;

sheet.getRange("A4:B4").values = [["功能分类", "功能数量"]];
sheet.getRange(`A5:B${4 + categories.length}`).values = categories;
const totalRow = 5 + categories.length;
sheet.getRange(`A${totalRow}`).values = [["合计"]];
sheet.getRange(`B${totalRow}`).formulas = [[`=SUM(B5:B${totalRow - 1})`]];

sheet.getRange("A4:B4").format = {
  fill: "#5B9BD5",
  font: { bold: true, color: "#FFFFFF" },
  horizontalAlignment: "center",
  verticalAlignment: "center",
};
sheet.getRange(`A4:B${totalRow}`).format.borders = {
  preset: "all",
  style: "thin",
  color: "#D6DEE8",
};
sheet.getRange(`A${totalRow}:B${totalRow}`).format = {
  fill: "#E2F0D9",
  font: { bold: true, color: "#375623" },
};
sheet.getRange(`B5:B${totalRow}`).format.numberFormat = "0";
sheet.getRange(`B5:B${totalRow}`).format.horizontalAlignment = "center";
sheet.getRange(`A4:B${totalRow}`).format.rowHeight = 25;
sheet.getRange("A:A").format.columnWidth = 28;
sheet.getRange("B:B").format.columnWidth = 16;

await fs.mkdir(outputDir, { recursive: true });
const preview = await workbook.render({ sheetName: "分类总览", range: `A1:B${totalRow}`, scale: 1, format: "png" });
await fs.writeFile(`${outputDir}/分类总览预览.png`, new Uint8Array(await preview.arrayBuffer()));

const inspection = await workbook.inspect({ kind: "region", sheetId: "分类总览", range: `A1:B${totalRow}`, maxChars: 3000 });
console.log(inspection.ndjson ?? inspection);
console.log(JSON.stringify({ categories: categories.length, total: categories.reduce((sum, item) => sum + item[1], 0) }));

const xlsx = await SpreadsheetFile.exportXlsx(workbook);
await xlsx.save(outputPath);
console.log(outputPath);

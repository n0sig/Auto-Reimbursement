using AutoReimbursement.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using Bold = DocumentFormat.OpenXml.Wordprocessing.Bold;
using BottomBorder = DocumentFormat.OpenXml.Wordprocessing.BottomBorder;
using Break = DocumentFormat.OpenXml.Wordprocessing.Break;
using Drawing = DocumentFormat.OpenXml.Wordprocessing.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using FontSize = DocumentFormat.OpenXml.Wordprocessing.FontSize;
using LeftBorder = DocumentFormat.OpenXml.Wordprocessing.LeftBorder;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using RightBorder = DocumentFormat.OpenXml.Wordprocessing.RightBorder;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using RunProperties = DocumentFormat.OpenXml.Wordprocessing.RunProperties;
using Table = DocumentFormat.OpenXml.Wordprocessing.Table;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;
using TopBorder = DocumentFormat.OpenXml.Wordprocessing.TopBorder;

namespace AutoReimbursement.Services;

/// <summary>
/// Stub implementation of document generation service.
/// The actual document generation logic is NOT implemented yet.
/// This service provides the API structure and validation only.
/// </summary>
public class DocumentGenerationService : IDocumentGenerationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DocumentGenerationService> _logger;

    public DocumentGenerationService(ApplicationDbContext dbContext, ILogger<DocumentGenerationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<GeneratedDocument>> GenerateDocumentsAsync(IEnumerable<int> invoiceIds,
        IEnumerable<DocumentType> documentTypes)
    {
        var invoiceIdList = invoiceIds?.ToList() ?? new List<int>();
        var documentTypeList = documentTypes?.ToList() ?? new List<DocumentType>();

        if (!invoiceIdList.Any())
        {
            throw new ArgumentException("At least one invoice ID must be provided.", nameof(invoiceIds));
        }

        if (!documentTypeList.Any())
        {
            throw new ArgumentException("At least one document type must be provided.", nameof(documentTypes));
        }

        // Fetch all invoices in a single query
        var invoices = await _dbContext.Invoices
            .Include(i => i.InvoiceItems)
            .Include(i => i.Payer)
            .Where(i => invoiceIdList.Contains(i.Id))
            .ToListAsync();

        // Validate all invoices exist
        if (invoices.Count != invoiceIdList.Count)
        {
            var foundIds = invoices.Select(i => i.Id).ToHashSet();
            var missingIds = invoiceIdList.Where(id => !foundIds.Contains(id)).ToList();
            throw new ArgumentException($"Invoices not found: {string.Join(", ", missingIds)}", nameof(invoiceIds));
        }

        // Validate all invoices are Material type
        var nonMaterialInvoices = invoices.Where(i => i.Type != InvoiceType.Material).ToList();
        if (nonMaterialInvoices.Any())
        {
            var invalidIds = nonMaterialInvoices.Select(i => i.Id).ToList();
            throw new ArgumentException(
                $"Document generation is only supported for Material type invoices. Invalid invoices: {string.Join(", ", invalidIds)}",
                nameof(invoiceIds));
        }

        _logger.LogInformation(
            "Generating {DocumentTypeCount} document type(s) for {InvoiceCount} invoice(s): {InvoiceIds}",
            documentTypeList.Count, invoices.Count, string.Join(", ", invoiceIdList));

        // Generate each document type
        var documents = new List<GeneratedDocument>();
        foreach (var documentType in documentTypeList)
        {
            var document = await GenerateDocumentFromInvoices(invoices, documentType);
            documents.Add(document);
        }

        return documents;
    }

    /// <inheritdoc />
    public async Task<List<DocumentType>> GetSupportedDocumentTypesAsync(int invoiceId)
    {
        var invoice = await _dbContext.Invoices.FindAsync(invoiceId);

        if (invoice == null || invoice.Type != InvoiceType.Material)
        {
            return new List<DocumentType>();
        }

        return new List<DocumentType>
        {
            DocumentType.WarehouseInOut,
            DocumentType.PurchaseRegistration
        };
    }

    /// <inheritdoc />
    public async Task<bool> IsDocumentGenerationSupportedAsync(int invoiceId)
    {
        var invoice = await _dbContext.Invoices.FindAsync(invoiceId);
        return invoice != null && invoice.Type == InvoiceType.Material;
    }

    private async Task<GeneratedDocument> GenerateDocumentFromInvoices(List<Invoice> invoices, DocumentType documentType)
    {
        _logger.LogInformation("Generating {DocumentType} document for {Count} invoice(s)",
            documentType, invoices.Count);

        // TODO: Implement actual document generation logic
        // For now, return a placeholder document indicating the feature is not yet implemented
        var invoiceSerials = string.Join(", ", invoices.Select(i => i.Serial ?? $"ID:{i.Id}"));
        var content = System.Text.Encoding.UTF8.GetBytes(
            $"Document generation for {documentType} is not yet implemented.\n" +
            $"Invoice Count: {invoices.Count}\n" +
            $"Invoice Serials: {invoiceSerials}");

        var incremental = 1;
        if (documentType == DocumentType.WarehouseInOut)
            content = await GenerateWarehouseInAndOutDocument(
                Path.GetTempFileName(),
                invoices.SelectMany(i => i.InvoiceItems).Select(item => new SlipItem(
                    incremental++,
                    item.Name,
                    item.Specification ?? "",
                    item.Unit ?? "",
                    item.Amount ?? 1,
                    item.Pretax,
                    item.Tax,
                    item.Pretax + item.Tax,
                    invoiceSerials
                )).ToList(),
                "C:\\Users\\yhszj\\Downloads\\sig_purchase.png", "C:\\Users\\yhszj\\Downloads\\sig_custody.png", "C:\\Users\\yhszj\\Downloads\\sig_audit.png"
            );

        return new GeneratedDocument
        {
            FileName = GenerateFileName(invoices, documentType),
            Content = content,
            ContentType = GetContentType(documentType),
            DocumentType = documentType
        };
    }

    private static string GenerateFileName(List<Invoice> invoices, DocumentType documentType)
    {
        var date = DateTime.Now.ToString("yyyyMMdd");

        // Single invoice: use invoice serial
        if (invoices.Count == 1)
        {
            var invoice = invoices[0];
            var serial = !string.IsNullOrEmpty(invoice.Serial) ? invoice.Serial : $"Invoice_{invoice.Id}";
            var invoiceDate = invoice.Date?.ToString("yyyyMMdd") ?? date;

            return documentType switch
            {
                DocumentType.WarehouseInOut => $"WarehouseInOut_{serial}_{invoiceDate}.docx",
                DocumentType.PurchaseRegistration => $"PurchaseRegistration_{serial}_{invoiceDate}.xlsx",
                DocumentType.InvoicePdf => $"Invoice_{serial}_{invoiceDate}.pdf",
                _ => $"Document_{serial}_{invoiceDate}.txt"
            };
        }

        // Multiple invoices: use combined naming
        var count = invoices.Count;
        return documentType switch
        {
            DocumentType.WarehouseInOut => $"WarehouseInOut_Combined_{count}invoices_{date}.xlsx",
            DocumentType.PurchaseRegistration => $"PurchaseRegistration_Combined_{count}invoices_{date}.xlsx",
            DocumentType.InvoicePdf => $"Invoices_Combined_{count}invoices_{date}.pdf",
            _ => $"Document_Combined_{count}invoices_{date}.txt"
        };
    }

    private static string GetContentType(DocumentType documentType)
    {
        return documentType switch
        {
            DocumentType.WarehouseInOut => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            DocumentType.PurchaseRegistration => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            DocumentType.InvoicePdf => "application/pdf",
            _ => "application/octet-stream"
        };
    }

    public static async Task<byte[]> GenerateWarehouseInAndOutDocument(
        string filepath,
        List<SlipItem> items,
        string sig1Path, string sig2Path, string sig3Path)
    {
        using (WordprocessingDocument package =
               WordprocessingDocument.Create(filepath, WordprocessingDocumentType.Document))
        {
            MainDocumentPart mainPart = package.AddMainDocumentPart();
            mainPart.Document = new Document();
            Body body = mainPart.Document.AppendChild(new Body());

            // --- 1. Warehouse Copy ---
            CreateSlipSection(mainPart, body, "浙江大学科研材料入库单（仓库联）", items, true, "采购", "验收保管", "审核", sig1Path,
                sig2Path, sig3Path);
            // Page Break
            body.AppendChild(new Paragraph(new Run(new Break() { Type = BreakValues.Page })));

            // --- 2. Financial Copy ---
            CreateSlipSection(mainPart, body, "浙江大学科研材料入库单（财务记账联）", items, true, "采购", "验收保管", "审核", sig1Path,
                sig2Path, sig3Path);
            // Page Break
            body.AppendChild(new Paragraph(new Run(new Break() { Type = BreakValues.Page })));

            // --- 3. Outgoing Slip ---
            CreateSlipSection(mainPart, body, "浙江大学科研材料出库单", items, false, "领用", "保管", "审核", sig1Path, sig2Path,
                sig3Path);

            // --- Page Margins ---
            SectionProperties sectPr = new SectionProperties();
            PageMargin pageMargin = new PageMargin()
            {
                Top = 720,
                Right = 720,
                Bottom = 720,
                Left = 720
            };
            sectPr.Append(pageMargin);
            body.Append(sectPr);

            mainPart.Document.Save();
        }
        return await File.ReadAllBytesAsync(filepath);
    }
    
    private static void CreateSlipSection(
        MainDocumentPart mainPart, Body body,
        string titleText, List<SlipItem> items, bool isIncoming,
        string label1, string label2, string label3,
        string imgPath1, string imgPath2, string imgPath3)
    {
        // 1. Title: SimHei (黑体), Size 32 (三号/16pt)
        Paragraph title = CreateParagraph(titleText, JustificationValues.Center, true, "32", "SimHei");
        title.ParagraphProperties!.SpacingBetweenLines =
            new SpacingBetweenLines() { Before = "240", After = "240" };
        body.Append(title);

        // 2. Metadata: SimSun (宋体), Size 24 (小四/12pt)
        Paragraph infoLine =
            new Paragraph(new ParagraphProperties(new Justification() { Val = JustificationValues.Both }));
        Run rInfo = CreateRun($"项目（课题）名称及代码：　　　　　　　　　　　　　　　　　　　", false, "24", "SimSun");

        // Add tabs for spacing
        rInfo.Append(new TabChar(), new TabChar(), new TabChar());

        var now = DateTime.Now;
        var dateString = $"{now.Year} 年 {now.Month} 月 {now.Day} 日";
        Run rDate = CreateRun(dateString, false, "24", "SimSun");
        infoLine.Append(rInfo);
        infoLine.Append(rDate);
        body.Append(infoLine);

        // 3. Table
        Table table = new Table();
        TableProperties tblProps = new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
            ),
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }
        );
        table.AppendChild(tblProps);

        // Headers: Bold, SimSun, Size 24
        TableRow headerRow = new TableRow();
        string[] headers =
        {
            "编号", "品名", "型号\n（规格）", "单位", isIncoming ? "入库\n数量" : "出库\n数量", "金额（元）\n*不含税额", "税额\n（元）", "金额小计\n（元）",
            "发票号"
        };
        foreach (var h in headers) headerRow.Append(CreateCell(h, true));
        table.Append(headerRow);

        // Data: SimSun, Size 24
        decimal numericTotal = 0;
        foreach (var item in items)
        {
            numericTotal += item.Total;
            TableRow tr = new TableRow();
            tr.Append(CreateCell(item.Id.ToString()));
            tr.Append(CreateCell(item.Name));
            tr.Append(CreateCell(item.Model));
            tr.Append(CreateCell(item.Unit));
            tr.Append(CreateCell(item.Qty.ToString()));
            tr.Append(CreateCell(item.Pretax.ToString("F2")));
            tr.Append(CreateCell(item.Tax.ToString("F2")));
            tr.Append(CreateCell(item.Total.ToString("F2")));
            tr.Append(CreateCell(item.Invoice));
            table.Append(tr);
        }

        // Total Row
        TableRow totalRow = new TableRow();
        TableCell labelCell = CreateCell($"　金额合计（大写）{numericTotal.ToChineseMoney()}", false);
        labelCell.GetFirstChild<TableCellProperties>()!.Append(new GridSpan() { Val = 7 });
        totalRow.Append(labelCell);
        totalRow.Append(CreateCell(numericTotal.ToString("F2")));
        totalRow.Append(CreateCell(""));
        table.Append(totalRow);

        // Signatures: SimSun, Size 24
        TableRow sigRow = new TableRow();
        TableCell sigCell = CreateSignatureCell(mainPart, label1, label2, label3, imgPath1, imgPath2, imgPath3);
        sigCell.GetFirstChild<TableCellProperties>()!.Append(new GridSpan() { Val = 9 });
        sigRow.Append(sigCell);
        table.Append(sigRow);

        body.Append(table);

        // 4. Footer Notes: SimSun, Size 24
        Paragraph notePara = new Paragraph(new ParagraphProperties(new SpacingBetweenLines() { Before = "100" }));
        if (isIncoming) {
            Run noteRun1 = CreateRun("注：①本单一式两联，第一联为仓库联，第二联为办理付款及财务记账联；", false, "18", "SimSun");
            noteRun1.Append(new Break());
            noteRun1.Append(new Text("　　②入库保管需为同一人，指定专人负责；采购、验收保管不能为同一人。"));
            notePara.Append(noteRun1);
        } else {
            Run noteRun1 = CreateRun("　注：出入库保管需为同一人，指定专人负责；领用、保管不能为同一人。", false, "18", "SimSun");
            notePara.Append(noteRun1);
        }
        body.Append(notePara);
    }

    private static Run CreateImageRun(MainDocumentPart mainPart, string imagePath)
    {
        ImagePart imagePart = mainPart.AddImagePart(ImagePartType.Png);
        using (FileStream stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
        {
            imagePart.FeedData(stream);
        }

        string relationshipId = mainPart.GetIdOfPart(imagePart);

        long cy = 400000; // ~1.1 cm
        long cx = 400000;

        try
        {
            using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
            {
                var dims = GetImageDimensions(stream);
                if (dims.Width > 0 && dims.Height > 0)
                {
                    double ratio = (double)dims.Width / dims.Height;
                    cx = (long)(cy * ratio);
                }
            }
        }
        catch
        {
        }

        var element =
            new Drawing(
                new DW.Inline(
                    new DW.Extent() { Cx = cx, Cy = cy },
                    new DW.EffectExtent() { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                    new DW.DocProperties() { Id = (UInt32Value)1U, Name = "Signature" },
                    new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks()
                        { NoChangeAspect = true }),
                    new A.Graphic(
                        new A.GraphicData(
                            new PIC.Picture(
                                new PIC.NonVisualPictureProperties(
                                    new PIC.NonVisualDrawingProperties() { Id = (UInt32Value)0U, Name = "Sig" },
                                    new PIC.NonVisualPictureDrawingProperties()),
                                new PIC.BlipFill(
                                    new A.Blip(new A.BlipExtensionList(new A.BlipExtension()
                                        { Uri = "{28A0092B-C50C-407E-A947-70E740481C1C}" }))
                                    {
                                        Embed = relationshipId, CompressionState = A.BlipCompressionValues.Print
                                    },
                                    new A.Stretch(new A.FillRectangle())),
                                new PIC.ShapeProperties(
                                    new A.Transform2D(new A.Offset() { X = 0L, Y = 0L },
                                        new A.Extents() { Cx = cx, Cy = cy }),
                                    new A.PresetGeometry(new A.AdjustValueList())
                                        { Preset = A.ShapeTypeValues.Rectangle }))
                        ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
                ) { DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U }
            );

        return new Run(element);
    }

    public static (int Width, int Height) GetImageDimensions(Stream stream)
    {
        byte[] header = new byte[8];
        if (stream.Read(header, 0, 8) < 8) return (0, 0);

        if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) // PNG
        {
            stream.Seek(16, SeekOrigin.Begin);
            byte[] buffer = new byte[8];
            stream.Read(buffer, 0, 8);
            int width = (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];
            int height = (buffer[4] << 24) | (buffer[5] << 16) | (buffer[6] << 8) | buffer[7];
            return (width, height);
        }
        else if (header[0] == 0xFF && header[1] == 0xD8) // JPG
        {
            stream.Seek(2, SeekOrigin.Begin);
            int b = stream.ReadByte();
            while (b != -1)
            {
                if (b == 0xFF)
                {
                    int marker = stream.ReadByte();
                    if (marker >= 0xC0 && marker <= 0xC3)
                    {
                        stream.ReadByte();
                        stream.ReadByte();
                        stream.ReadByte();
                        int h = (stream.ReadByte() << 8) | stream.ReadByte();
                        int w = (stream.ReadByte() << 8) | stream.ReadByte();
                        return (w, h);
                    }

                    int len = (stream.ReadByte() << 8) | stream.ReadByte();
                    stream.Seek(len - 2, SeekOrigin.Current);
                }

                b = stream.ReadByte();
            }
        }

        return (0, 0);
    }

    // --- Styled Helper Methods ---

    private static TableCell CreateCell(string text, bool isHeader = false)
    {
        TableCell cell = new TableCell();
        TableCellProperties props = new TableCellProperties();
        props.Append(new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center });
        cell.Append(props);

        Paragraph p = new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Center },
            new SpacingBetweenLines { After = "0", Before = "0", Line = "240" }));

        // Apply SimSun (宋体) and Size 24 (12pt) to table cells
        Run r = CreateRun(text, isHeader, "24", "SimSun");

        p.Append(r);
        cell.Append(p);
        return cell;
    }

    private static TableCell CreateSignatureCell(MainDocumentPart mainPart, string label1, string label2,
        string label3, string imgPath1, string imgPath2, string imgPath3)
    {
        TableCell cell = new TableCell();
        TableCellProperties props = new TableCellProperties();
        props.Append(new TableCellVerticalAlignment() { Val = TableVerticalAlignmentValues.Center });
        cell.Append(props);

        Paragraph p = new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Center },
            new SpacingBetweenLines { After = "0", Before = "0", Line = "240" }));

        p.Append(CreateRun($"{label1}（签字）： ", false, "24", "SimSun"));
        if (File.Exists(imgPath1)) p.Append(CreateImageRun(mainPart, imgPath1));
        else p.Append(new Run(new Text("          ")));

        p.Append(new Run(new Text("      "))); // Spacer

        p.Append(CreateRun($"{label2}（签字）： ", false, "24", "SimSun"));
        if (File.Exists(imgPath2)) p.Append(CreateImageRun(mainPart, imgPath2));
        else p.Append(new Run(new Text("          ")));

        p.Append(new Run(new Text("      "))); // Spacer

        p.Append(CreateRun($"{label3}（签字）： ", false, "24", "SimSun"));
        if (File.Exists(imgPath3)) p.Append(CreateImageRun(mainPart, imgPath3));

        cell.Append(p);
        return cell;
    }

    private static Paragraph CreateParagraph(string text, JustificationValues align, bool bold, string fontSize,
        string fontName)
    {
        Paragraph p = new Paragraph();
        ParagraphProperties pp = new ParagraphProperties();
        pp.Justification = new Justification() { Val = align };
        p.Append(pp);

        Run r = CreateRun(text, bold, fontSize, fontName);
        p.Append(r);
        return p;
    }

    private static Run CreateRun(string text, bool bold, string fontSize, string fontName)
    {
        Run r = new Run();
        RunProperties rp = new RunProperties();

        if (bold) rp.Bold = new Bold();
        rp.FontSize = new FontSize() { Val = fontSize };

        // CORRECTED HERE: "HighAnsi" instead of "HAnsi"
        rp.RunFonts = new RunFonts() { Ascii = fontName, EastAsia = fontName, HighAnsi = fontName };

        r.Append(rp);

        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            r.Append(new Text(lines[i]));
            if (i < lines.Length - 1) r.Append(new Break());
        }

        return r;
    }

    public class SlipItem(
        int id,
        string name,
        string model,
        string unit,
        int qty,
        decimal pretax,
        decimal tax,
        decimal total,
        string invoice)
    {
        public int Id { get; } = id;
        public string Name { get; } = name;
        public string Model { get; } = model;
        public string Unit { get; } = unit;
        public int Qty { get; } = qty;
        public decimal Pretax { get; } = pretax;
        public decimal Tax { get; } = tax;
        public decimal Total { get; } = total;
        public string Invoice { get; } = invoice;
    }
}

public static class RmbConverter
{
    private static readonly string[] CnUpperNumber = ["零", "壹", "贰", "叁", "肆", "伍", "陆", "柒", "捌", "玖"];
    private static readonly string[] CnUpperUnit = ["", "拾", "佰", "仟"];
    private static readonly string[] CnGroupUnit = ["", "万", "亿", "兆"];

    public static string ToChineseMoney(this decimal money)
    {
        if (money == 0) return "零元整";

        StringBuilder sb = new StringBuilder();
        if (money < 0)
        {
            sb.Append("负");
            money = Math.Abs(money);
        }

        money = Math.Round(money, 2);

        long intPart = (long)Math.Floor(money);
        long decimalPart = (long)(money * 100) % 100;

        if (intPart > 0)
        {
            sb.Append(ConvertIntegerPart(intPart));
            sb.Append("元");
        }
        else if (decimalPart > 0)
        {
            sb.Append("零元");
        }

        if (decimalPart == 0)
        {
            sb.Append("整");
        }
        else
        {
            sb.Append(ConvertDecimalPart(decimalPart));
        }

        return sb.ToString();
    }

    private static string ConvertIntegerPart(long number)
    {
        StringBuilder buffer = new StringBuilder();
        int groupIndex = 0;
        bool zeroPending = false; // 标记跨组的零

        while (number > 0)
        {
            int section = (int)(number % 10000);

            if (section > 0)
            {
                string sectionStr = SectionToChinese(section);

                // 跨组补零逻辑
                if (zeroPending)
                {
                    buffer.Insert(0, "零");
                    zeroPending = false;
                }

                if (groupIndex > 0)
                {
                    buffer.Insert(0, CnGroupUnit[groupIndex]);
                }

                buffer.Insert(0, sectionStr);

                // 如果当前组不满1000（例如0500万），说明高位有0，需要标记
                if (section < 1000)
                {
                    zeroPending = true;
                }
            }
            else if (section == 0)
            {
                // 如果中间有空组（例如 1,0000,0001 中的万位），需要标记补零
                if (buffer.Length > 0)
                {
                    zeroPending = true;
                }
            }

            number /= 10000;
            groupIndex++;
        }

        return buffer.ToString();
    }

    /// <summary>
    /// 修正后的核心方法：将4位以内的数字转换为中文
    /// </summary>
    private static string SectionToChinese(int section)
    {
        StringBuilder sb = new StringBuilder();
        int unitPos = 0;
        bool zeroFlag = false;

        while (section > 0)
        {
            int digit = section % 10;

            if (digit == 0)
            {
                // 【关键修正】
                // 只有当低位已经有内容时（sb.Length > 0），这个0才是“中间的0”，需要标记。
                // 如果 sb 是空的，说明这是末尾的0（如 1680 的个位），应该忽略。
                if (sb.Length > 0)
                {
                    zeroFlag = true;
                }
            }
            else
            {
                if (zeroFlag)
                {
                    sb.Insert(0, "零");
                    zeroFlag = false;
                }

                if (unitPos > 0)
                {
                    sb.Insert(0, CnUpperUnit[unitPos]);
                }

                sb.Insert(0, CnUpperNumber[digit]);
            }

            section /= 10;
            unitPos++;
        }

        return sb.ToString();
    }

    private static string ConvertDecimalPart(long decimalNum)
    {
        StringBuilder sb = new StringBuilder();
        int jiao = (int)(decimalNum / 10);
        int fen = (int)(decimalNum % 10);

        if (jiao > 0)
        {
            sb.Append(CnUpperNumber[jiao]);
            sb.Append("角");
        }
        else if (fen > 0)
        {
            // 角位是0但分位不是0
            sb.Append("零");
        }

        if (fen > 0)
        {
            sb.Append(CnUpperNumber[fen]);
            sb.Append("分");
        }

        return sb.ToString();
    }
}
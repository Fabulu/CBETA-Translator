//! Bilingual PDF generation with alternating Chinese/English paragraphs
//! 
//! Creates high-quality PDFs with professional typography, alternating paragraph layout,
//! and hOCR layers for text accessibility.

use anyhow::{Result, anyhow};
use crate::fonts::FontContext;
use crate::typography::{TextLayoutEngine, FormattedParagraph, FormattedLine};
use crate::hocr_layer::{HocrGenerator, HocrPage};
use lopdf::{
    Document, Object, Dictionary, Stream, StringFormat,
    content::{Content, Operation},
    ObjectId,
};
use std::collections::HashMap;

/// Bilingual PDF generator
pub struct BilingualPdfGenerator {
    font_context: FontContext,
    layout_engine: TextLayoutEngine,
    hocr_generator: HocrGenerator,
    document: Document,
    font_objects: HashMap<String, ObjectId>,
    pages_id: ObjectId,
}

impl BilingualPdfGenerator {
    // Extra inner guard so text never hugs or clips at page edges.
    const SAFE_INSET_X: f32 = 10.0;
    const SAFE_INSET_Y: f32 = 10.0;

    fn safe_content_area(&self) -> (f32, f32, f32, f32) {
        let (x, y, w, h) = self.font_context.content_area();
        let safe_x = x + Self::SAFE_INSET_X;
        let safe_y = y + Self::SAFE_INSET_Y;
        let safe_w = (w - 2.0 * Self::SAFE_INSET_X).max(120.0);
        let safe_h = (h - 2.0 * Self::SAFE_INSET_Y).max(120.0);
        (safe_x, safe_y, safe_w, safe_h)
    }

    /// Create a new bilingual PDF generator
    pub fn new(font_context: FontContext) -> Self {
        let layout_engine = crate::typography::create_layout_engine(font_context.clone());
        let hocr_generator = crate::hocr_layer::create_hocr_generator();
        
        Self {
            font_context,
            layout_engine,
            hocr_generator,
            document: Document::new(),
            font_objects: HashMap::new(),
            pages_id: (0, 0), // Will be set properly in initialize_document
        }
    }
    
    /// Generate a bilingual PDF with alternating paragraphs
    pub fn generate_bilingual_pdf(
        &mut self,
        chinese_sections: &[String],
        english_sections: &[String],
        output_path: &str,
    ) -> Result<()> {
        if chinese_sections.len() != english_sections.len() {
            return Err(anyhow!(
                "Chinese and English sections must have the same length: {} vs {}",
                chinese_sections.len(),
                english_sections.len()
            ));
        }
        
        // Initialize PDF document
        self.initialize_document()?;
        
        // Create alternating paragraph layout
        let all_paragraphs = self.create_alternating_layout(chinese_sections, english_sections)?;
        
        // Generate pages
        println!("DEBUG: About to create pages from {} paragraphs", all_paragraphs.len());
        let pages = self.create_pages_from_paragraphs(all_paragraphs)?;
        println!("DEBUG: Created {} pages", pages.len());

        // Add hOCR layer
        self.add_hocr_layer(&pages)?;

        // Save document
        println!("DEBUG: Document has {} objects, pages_id: {:?}, {} actual pages in Kids array", 
                 self.document.objects.len(), 
                 self.pages_id,
                 self.get_page_count()?);
        self.save_document(output_path)?;

        Ok(())
    }

    /// Generate a bilingual PDF with true side-by-side columns.
    pub fn generate_bilingual_pdf_side_by_side(
        &mut self,
        chinese_sections: &[String],
        english_sections: &[String],
        output_path: &str,
    ) -> Result<()> {
        if chinese_sections.len() != english_sections.len() {
            return Err(anyhow!(
                "Chinese and English sections must have the same length: {} vs {}",
                chinese_sections.len(),
                english_sections.len()
            ));
        }

        self.initialize_document()?;
        let pages = self.create_pages_side_by_side(chinese_sections, english_sections)?;
        self.add_hocr_layer(&pages)?;
        self.save_document(output_path)?;
        Ok(())
    }
    
    /// Initialize PDF document with fonts and metadata
    fn initialize_document(&mut self) -> Result<()> {
        // Add fonts to document
        let _chinese_id = self.add_font_to_document("chinese")?;
        let _english_id = self.add_font_to_document("english")?;

        // Create pages structure
        let mut pages_dict = Dictionary::new();
        pages_dict.set("Type", Object::Name(b"Pages".to_vec()));
        pages_dict.set("Kids", Object::Array(vec![])); // Will be populated later
        pages_dict.set("Count", Object::Integer(0)); // Initialize count to 0
        let pages_id = self.document.add_object(Object::Dictionary(pages_dict));

        // Create info dictionary
        let mut info_dict = Dictionary::new();
        info_dict.set("Producer", Object::string_literal("CBETA Bilingual PDF Creator"));
        info_dict.set("Creator", Object::string_literal("CBETA Project"));
        let info_id = self.document.add_object(Object::Dictionary(info_dict));

        // Create catalog with required root and metadata
        let mut catalog_dict = Dictionary::new();
        catalog_dict.set("Type", Object::Name(b"Catalog".to_vec()));
        catalog_dict.set("Pages", Object::Reference(pages_id));
        
        let catalog_id = self.document.add_object(Object::Dictionary(catalog_dict));

        // Set catalog as root and info in trailer
        self.document.trailer.set(b"Root", Object::Reference(catalog_id));
        self.document.trailer.set(b"Info", Object::Reference(info_id));

        // Store pages ID for later use
        self.pages_id = pages_id;

        Ok(())
    }
    
    /// Create alternating layout: Chinese #1 → English #1 → Chinese #2 → English #2
    fn create_alternating_layout(
        &mut self,
        chinese_sections: &[String],
        english_sections: &[String],
    ) -> Result<Vec<FormattedParagraph>> {
        let mut all_paragraphs = Vec::new();
        
        let (content_x, content_y, content_width, _content_height) = self.safe_content_area();
        let mut current_y = content_y;
        
        // Add spacing between alternating paragraphs
        let paragraph_spacing = self.font_context.get_line_height(true) * 0.5;
        
        for (index, (chinese_text, english_text)) in chinese_sections.iter().zip(english_sections.iter()).enumerate() {
            // Add Chinese paragraph
            if !chinese_text.trim().is_empty() {
                let chinese_para = self.layout_engine.layout_paragraph(
                    chinese_text,
                    content_x,
                    current_y,
                    content_width,
                    true, // is_chinese
                )?;
                
                current_y += chinese_para.height + paragraph_spacing;
                all_paragraphs.push(chinese_para);
            }
            
            // Add English paragraph
            if !english_text.trim().is_empty() {
                let english_para = self.layout_engine.layout_paragraph(
                    english_text,
                    content_x,
                    current_y,
                    content_width,
                    false, // is_chinese
                )?;
                
                current_y += english_para.height + paragraph_spacing;
                all_paragraphs.push(english_para);
            }
            
            // Add section separator (optional)
            if index < chinese_sections.len() - 1 {
                current_y += paragraph_spacing * 2.0; // Extra space between sections
            }
        }
        
        Ok(all_paragraphs)
    }
    
    /// Create PDF pages from formatted paragraphs
    fn create_pages_from_paragraphs(&mut self, paragraphs: Vec<FormattedParagraph>) -> Result<Vec<HocrPage>> {
        let mut pages = Vec::new();
        let (_content_x, _content_y, _content_width, content_height) = self.safe_content_area();
        
        // Group paragraphs into pages
        let mut current_page_paragraphs = Vec::new();
        let mut current_page_height = 0.0;
        
        for paragraph in &paragraphs {
            if current_page_height + paragraph.height > content_height && !current_page_paragraphs.is_empty() {
                // Current page is full, create it
                let page = self.create_single_page(&current_page_paragraphs)?;
                pages.push(page);
                
                // Start new page
                current_page_paragraphs.clear();
                current_page_height = 0.0;
            }
            
            current_page_paragraphs.push(paragraph.clone());
            current_page_height += paragraph.height;
        }
        
        // Create the last page if there are remaining paragraphs
        if !current_page_paragraphs.is_empty() {
            let page = self.create_single_page(&current_page_paragraphs)?;
            pages.push(page);
        }
        
        Ok(pages)
    }

    /// Create pages for side-by-side layout (left column Chinese, right column English).
    fn create_pages_side_by_side(
        &mut self,
        chinese_sections: &[String],
        english_sections: &[String],
    ) -> Result<Vec<HocrPage>> {
        let mut pages = Vec::new();
        let (content_x, content_y, content_width, content_height) = self.safe_content_area();
        let page_bottom = content_y + content_height;

        let column_gutter = 24.0_f32;
        let column_width = ((content_width - column_gutter) / 2.0).max(120.0);
        let right_column_x = content_x + column_width + column_gutter;
        let row_spacing = (self.font_context.get_line_height(true) * self.font_context.paragraph_spacing.max(0.2))
            .max(4.0);

        let mut current_y = content_y;
        let mut current_page_paragraphs: Vec<FormattedParagraph> = Vec::new();

        for (zh_text, en_text) in chinese_sections.iter().zip(english_sections.iter()) {
            let (mut zh_para, mut en_para, mut row_height) = self.layout_side_by_side_row(
                zh_text,
                en_text,
                content_x,
                right_column_x,
                current_y,
                column_width,
            )?;

            if current_y + row_height > page_bottom && !current_page_paragraphs.is_empty() {
                let page = self.create_single_page(&current_page_paragraphs)?;
                pages.push(page);
                current_page_paragraphs.clear();
                current_y = content_y;

                let relaid = self.layout_side_by_side_row(
                    zh_text,
                    en_text,
                    content_x,
                    right_column_x,
                    current_y,
                    column_width,
                )?;
                zh_para = relaid.0;
                en_para = relaid.1;
                row_height = relaid.2;
            }

            if let Some(p) = zh_para {
                current_page_paragraphs.push(p);
            }
            if let Some(p) = en_para {
                current_page_paragraphs.push(p);
            }

            current_y += row_height + row_spacing;
        }

        if !current_page_paragraphs.is_empty() {
            let page = self.create_single_page(&current_page_paragraphs)?;
            pages.push(page);
        }

        Ok(pages)
    }

    fn layout_side_by_side_row(
        &mut self,
        zh_text: &str,
        en_text: &str,
        left_x: f32,
        right_x: f32,
        row_y: f32,
        column_width: f32,
    ) -> Result<(Option<FormattedParagraph>, Option<FormattedParagraph>, f32)> {
        let zh_para = if zh_text.trim().is_empty() {
            None
        } else {
            Some(self.layout_engine.layout_paragraph(
                zh_text,
                left_x,
                row_y,
                column_width,
                true,
            )?)
        };

        let en_para = if en_text.trim().is_empty() {
            None
        } else {
            Some(self.layout_engine.layout_paragraph(
                en_text,
                right_x,
                row_y,
                column_width,
                false,
            )?)
        };

        let row_height = zh_para.as_ref().map(|p| p.height).unwrap_or(0.0)
            .max(en_para.as_ref().map(|p| p.height).unwrap_or(0.0))
            .max(self.font_context.get_line_height(true));

        Ok((zh_para, en_para, row_height))
    }
    
    /// Create a single PDF page from paragraphs
    fn create_single_page(&mut self, paragraphs: &[FormattedParagraph]) -> Result<HocrPage> {
        let page_id = self.document.new_object_id();
        
        // Create page content
        let mut content = Content {
            operations: Vec::new(),
        };
        
        // Add each paragraph to the page content
        for paragraph in paragraphs {
            self.add_paragraph_to_content(&mut content, paragraph)?;
        }
        
        // Create page object
        let mut page_dict = Dictionary::new();
        page_dict.set("Type", Object::Name(b"Page".to_vec()));
        page_dict.set("Parent", Object::Reference(self.pages_id)); // Reference to pages object
        page_dict.set("Resources", self.create_resources_dict()?);
        page_dict.set("MediaBox", Object::Array(vec![
            Object::Integer(0),
            Object::Integer(0),
            Object::Real(self.font_context.page_width),
            Object::Real(self.font_context.page_height),
        ]));

        let content_stream = Stream::new(Dictionary::new(), content.encode()?);
        let content_id = self.document.add_object(content_stream);
        page_dict.set("Contents", Object::Reference(content_id));

        // Add page to document
        self.document.objects.insert(page_id, Object::Dictionary(page_dict));

        // Add to pages tree
        self.add_page_to_tree(page_id)?;
        
        // Generate hOCR for this page
        let hocr_page = self.hocr_generator.generate_hocr(paragraphs)?;
        
        Ok(hocr_page)
    }
    
    /// Add a paragraph to the page content with professional typography
    fn add_paragraph_to_content(&mut self, content: &mut Content, paragraph: &FormattedParagraph) -> Result<()> {
        let font_name = if paragraph.is_chinese { "chinese" } else { "english" };
        let font_size = paragraph.font_size;

        content.operations.push(Operation::new("BT", vec![]));
        content.operations.push(Operation::new("Tf", vec![
            Object::Name(font_name.as_bytes().to_vec()),
            Object::Real(font_size),
        ]));

        for line in &paragraph.lines {
            // Use baseline positioning for proper leading
            let pdf_y = self.font_context.page_height - (paragraph.y + line.baseline);

            content.operations.push(Operation::new("Tm", vec![
                Object::Real(1.0), Object::Real(0.0),
                Object::Real(0.0), Object::Real(1.0),
                Object::Real(paragraph.x + line.x), Object::Real(pdf_y),
            ]));

            // Build TJ array with kerning + tracking
            let tj = self.build_tj_array(line)?;
            content.operations.push(Operation::new("TJ", vec![Object::Array(tj)]));
        }

        content.operations.push(Operation::new("ET", vec![]));
        Ok(())
    }

    /// Build TJ array with proper Chinese character handling
    fn build_tj_array(&mut self, line: &FormattedLine) -> Result<Vec<Object>> {
        let font = if line.is_chinese { 
            self.font_context.chinese_font.clone() 
        } else { 
            self.font_context.english_font.clone() 
        };
        let size = line.font_size;
        let tracking = if line.is_chinese { self.font_context.tracking_chinese } else { self.font_context.tracking_english };
        let _scale = size / font.units_per_em() as f32;

        let chars: Vec<char> = line.text.chars().collect();
        let mut tj = Vec::with_capacity(chars.len() * 2);

        // Create a map of space positions to adjustments
        let mut space_adjustments_map = std::collections::HashMap::new();
        for adj in &line.space_adjustments {
            space_adjustments_map.insert(adj.position, adj.adjustment_ratio);
        }

        let mut char_position = 0;
        for (i, &ch) in chars.iter().enumerate() {
            if line.is_chinese {
                let mut utf16be = Vec::new();
                for unit in ch.encode_utf16(&mut [0; 2]).iter().copied() {
                    utf16be.extend_from_slice(&unit.to_be_bytes());
                }
                tj.push(Object::String(utf16be, StringFormat::Hexadecimal));
            } else {
                // English path keeps literal single-byte behavior (WinAnsi-compatible text expected).
                let char_string = ch.to_string();
                let utf8_bytes = char_string.as_bytes();
                tj.push(Object::String(utf8_bytes.to_vec(), StringFormat::Literal));
            }

            if i < chars.len() - 1 {
                let mut adjust: f32 = 0.0;

                // Kerning
                if let Some(kern) = font.horizontal_kern(ch, chars[i + 1], size) {
                    adjust += kern * 1000.0 / font.units_per_em() as f32;
                }

                // Tracking
                adjust += tracking;

                // Apply space adjustment if this is a space character
                if ch == ' ' {
                    if let Some(&ratio) = space_adjustments_map.get(&char_position) {
                        // Convert adjustment ratio to thousandths of em
                        let space_width = self.font_context.calculate_text_width(" ", line.is_chinese);
                        let extra_space = space_width * ratio;
                        adjust += extra_space * 1000.0 / size;
                    }
                }

                tj.push(Object::Real(adjust));
            }

            // Update character position for space tracking
            if ch != ' ' {
                char_position += 1;
            }
        }
        Ok(tj)
    }

    /// Create resources dictionary for fonts
    fn create_resources_dict(&self) -> Result<Object> {
        let mut resources = Dictionary::new();
        let mut font_dict = Dictionary::new();

        for (font_name, &font_id) in &self.font_objects {
            font_dict.set(font_name.as_str(), Object::Reference(font_id));
        }

        resources.set("Font", Object::Dictionary(font_dict));
        Ok(Object::Dictionary(resources))
    }
    
    /// Add font to document with proper CJK composite font support.
    fn add_font_to_document(&mut self, name: &str) -> Result<ObjectId> {
        // Chinese must be a composite Type0 font with a CIDFont descendant.
        if name == "chinese" {
            let base_font_name = self.chinese_pdf_font_name();

            let mut font_descriptor = Dictionary::new();
            font_descriptor.set("Type", Object::Name(b"FontDescriptor".to_vec()));
            font_descriptor.set("FontName", Object::Name(base_font_name.clone().into_bytes()));
            font_descriptor.set("Flags", Object::Integer(4));
            font_descriptor.set("FontBBox", Object::Array(vec![
                Object::Integer(0),
                Object::Integer(-300),
                Object::Integer(1000),
                Object::Integer(1000),
            ]));
            font_descriptor.set("ItalicAngle", Object::Integer(0));
            font_descriptor.set("Ascent", Object::Integer(880));
            font_descriptor.set("Descent", Object::Integer(-120));
            font_descriptor.set("CapHeight", Object::Integer(700));
            font_descriptor.set("StemV", Object::Integer(80));

            let mut is_embedded = false;
            if let Some(font_file_obj) = self.create_embeddable_chinese_font_stream() {
                let font_stream_id = self.document.add_object(font_file_obj);
                font_descriptor.set("FontFile2", Object::Reference(font_stream_id));
                is_embedded = true;
            }
            let font_descriptor_id = self.document.add_object(Object::Dictionary(font_descriptor));

            let mut cidfont = Dictionary::new();
            cidfont.set("Type", Object::Name(b"Font".to_vec()));
            cidfont.set("Subtype", Object::Name(b"CIDFontType2".to_vec()));
            cidfont.set("BaseFont", Object::Name(base_font_name.clone().into_bytes()));
            cidfont.set("CIDSystemInfo", Object::Dictionary({
                let mut d = Dictionary::new();
                d.set("Registry", Object::string_literal("Adobe"));
                d.set("Ordering", Object::string_literal("Identity"));
                d.set("Supplement", Object::Integer(0));
                d
            }));
            cidfont.set("FontDescriptor", Object::Reference(font_descriptor_id));
            cidfont.set("DW", Object::Integer(1000));
            if is_embedded {
                // Embedded font: explicit map is safe and keeps glyph mapping deterministic.
                let cid_to_gid_map_id = self.document.add_object(self.create_cid_to_gid_map_stream());
                cidfont.set("CIDToGIDMap", Object::Reference(cid_to_gid_map_id));
            } else {
                // System font (e.g. TTC) is not embedded; do not force a possibly mismatched
                // custom CIDToGID map, let viewer resolve with identity mapping.
                cidfont.set("CIDToGIDMap", Object::Name(b"Identity".to_vec()));
            }
            let cidfont_id = self.document.add_object(Object::Dictionary(cidfont));

            let tounicode_id = self.document.add_object(self.create_identity_tounicode_cmap_stream());

            let mut type0 = Dictionary::new();
            type0.set("Type", Object::Name(b"Font".to_vec()));
            type0.set("Subtype", Object::Name(b"Type0".to_vec()));
            type0.set("BaseFont", Object::Name(base_font_name.into_bytes()));
            type0.set("Encoding", Object::Name(b"Identity-H".to_vec()));
            type0.set("DescendantFonts", Object::Array(vec![Object::Reference(cidfont_id)]));
            type0.set("ToUnicode", Object::Reference(tounicode_id));

            let font_id = self.document.add_object(Object::Dictionary(type0));
            self.font_objects.insert(name.to_string(), font_id);
            Ok(font_id)
        } else {
            // For English text, use a built-in PDF Type1 font for maximum viewer compatibility.
            let mut font_dict = Dictionary::new();
            font_dict.set("Type", Object::Name(b"Font".to_vec()));
            font_dict.set("Subtype", Object::Name(b"Type1".to_vec()));
            font_dict.set("BaseFont", Object::Name(b"Times-Roman".to_vec()));
            font_dict.set("Encoding", Object::Name(b"WinAnsiEncoding".to_vec()));
            
            let font_id = self.document.add_object(Object::Dictionary(font_dict));
            self.font_objects.insert(name.to_string(), font_id);
            Ok(font_id)
        }
    }

    fn sanitize_pdf_font_name(&self, raw: &str) -> String {
        let mut out = String::with_capacity(raw.len());
        for ch in raw.chars() {
            if ch.is_ascii_alphanumeric() || ch == '-' || ch == '_' {
                out.push(ch);
            } else if ch.is_whitespace() {
                out.push('-');
            }
        }
        if out.is_empty() {
            "CJKFont".to_string()
        } else {
            out
        }
    }

    fn chinese_pdf_font_name(&self) -> String {
        match self.font_context.chinese_font_name.as_str() {
            "Source Han Sans TC Regular" => "SourceHanSansTC-Regular".to_string(),
            "Microsoft JhengHei" => "MicrosoftJhengHei".to_string(),
            "Microsoft YaHei" => "MicrosoftYaHei".to_string(),
            "SimSun" => "SimSun".to_string(),
            "SimSun Bold" => "SimSun-Bold".to_string(),
            _ => self.sanitize_pdf_font_name(&self.font_context.chinese_font_name),
        }
    }

    fn create_identity_tounicode_cmap_stream(&self) -> Object {
        let cmap = b"/CIDInit /ProcSet findresource begin
12 dict begin
begincmap
/CIDSystemInfo
<< /Registry (Adobe)
/Ordering (UCS)
/Supplement 0
>> def
/CMapName /Adobe-Identity-UCS def
/CMapType 2 def
1 begincodespacerange
<0000> <FFFF>
endcodespacerange
1 beginbfrange
<0000> <FFFF> <0000>
endbfrange
endcmap
CMapName currentdict /CMap defineresource pop
end
end"
        .to_vec();
        Object::Stream(Stream::new(Dictionary::new(), cmap))
    }

    fn create_cid_to_gid_map_stream(&self) -> Object {
        // Build a full BMP CID -> glyph index map (2 bytes per CID).
        // CID code equals UTF-16 BMP code unit in our content stream.
        let mut map = vec![0u8; 65536 * 2];
        for cid in 0u32..=0xFFFF {
            if let Some(ch) = char::from_u32(cid) {
                let gid = self.font_context.chinese_font.lookup_glyph_index(ch);
                let offset = (cid as usize) * 2;
                map[offset] = (gid >> 8) as u8;
                map[offset + 1] = (gid & 0xFF) as u8;
            }
        }
        Object::Stream(Stream::new(Dictionary::new(), map))
    }

    fn create_embeddable_chinese_font_stream(&self) -> Option<Object> {
        let path = self.font_context.chinese_font_path.to_ascii_lowercase();
        if !path.ends_with(".ttf") {
            // Skip invalid embedding for TTC/OTF in FontFile2.
            return None;
        }

        let font_data = self.font_context.chinese_font_data.clone();
        if font_data.is_empty() {
            return None;
        }

        let mut stream_dict = Dictionary::new();
        stream_dict.set("Length1", Object::Integer(font_data.len() as i64));
        Some(Object::Stream(Stream::new(stream_dict, font_data)))
    }
    
    /// Add page to pages tree
    fn add_page_to_tree(&mut self, page_id: ObjectId) -> Result<()> {
        // Get the current count of kids to update the Count field
        let current_kids_count = {
            let pages_obj = self.document.get_object(self.pages_id)?;
            if let Object::Dictionary(ref pages_dict) = pages_obj {
                let kids = pages_dict.get(b"Kids")?.as_array()?;
                kids.len() + 1  // Adding one new page
            } else {
                return Err(anyhow!("Pages object is not a dictionary"));
            }
        };
        
        // Now update both Kids and Count
        let pages_obj = self.document.get_object_mut(self.pages_id)?;
        if let Object::Dictionary(ref mut pages_dict) = pages_obj {
            let kids = pages_dict.get_mut(b"Kids")?.as_array_mut()?;
            kids.push(Object::Reference(page_id));
            // Update the Count field to reflect the number of pages
            pages_dict.set("Count", Object::Integer(current_kids_count as i64));
        } else {
            return Err(anyhow!("Pages object is not a dictionary"));
        }

        Ok(())
    }
    
    /// Add hOCR layer to the document
    fn add_hocr_layer(&mut self, pages: &[HocrPage]) -> Result<()> {
        // Generate hOCR HTML
        let _hocr_html = self.hocr_generator.generate_hocr_html(pages)?;
        
        // Add hOCR as attachment or separate layer
        // This would be implemented with PDF annotations or attachments
        
        Ok(())
    }
    
    /// Save the document to file
    fn save_document(&mut self, output_path: &str) -> Result<()> {
        // Ensure the document is properly structured before saving
        self.document.compress();
        self.document.save(output_path)?;
        Ok(())
    }
    
    /// Helper function to get the page count from the Kids array
    fn get_page_count(&self) -> Result<usize> {
        let pages_obj = self.document.get_object(self.pages_id)?;
        if let Object::Dictionary(ref pages_dict) = pages_obj {
            let kids = pages_dict.get(b"Kids")?.as_array()?;
            Ok(kids.len())
        } else {
            Ok(0)
        }
    }
}

/// Create a bilingual PDF generator
pub fn create_bilingual_generator(font_context: FontContext) -> BilingualPdfGenerator {
    BilingualPdfGenerator::new(font_context)
}

/// Main entry point for bilingual PDF creation
pub fn create_bilingual_pdf(
    chinese_sections: &[String],
    english_sections: &[String],
    output_path: &str,
) -> Result<()> {
    let font_context = crate::fonts::initialize_fonts()?;
    let mut generator = create_bilingual_generator(font_context);
    
    generator.generate_bilingual_pdf(chinese_sections, english_sections, output_path)
}

/// Create bilingual PDF with custom font context (for professional typography)
pub fn create_bilingual_pdf_with_context(
    chinese_sections: &[String],
    english_sections: &[String],
    output_path: &str,
    font_context: &crate::fonts::FontContext,
) -> Result<()> {
    let mut generator = create_bilingual_generator(font_context.clone());
    
    generator.generate_bilingual_pdf(chinese_sections, english_sections, output_path)
}

/// Create bilingual PDF with side-by-side columns and custom font context.
pub fn create_bilingual_pdf_side_by_side_with_context(
    chinese_sections: &[String],
    english_sections: &[String],
    output_path: &str,
    font_context: &crate::fonts::FontContext,
) -> Result<()> {
    let mut generator = create_bilingual_generator(font_context.clone());
    generator.generate_bilingual_pdf_side_by_side(chinese_sections, english_sections, output_path)
}

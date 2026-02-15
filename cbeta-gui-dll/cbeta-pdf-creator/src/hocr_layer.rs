//! hOCR layer generation for PDF text accessibility
//! 
//! Creates hOCR markup that allows text selection, copying, and searching
//! in the generated PDF while maintaining the visual layout.

use crate::typography::{FormattedParagraph, FormattedLine};
use serde::{Deserialize, Serialize};
use anyhow::Result;

/// hOCR page structure
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct HocrPage {
    pub page_number: u32,
    pub bbox: (f32, f32, f32, f32), // x0, y0, x1, y1
    pub paragraphs: Vec<HocrParagraph>,
}

/// hOCR paragraph structure
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct HocrParagraph {
    pub id: String,
    pub bbox: (f32, f32, f32, f32), // x0, y0, x1, y1
    pub language: String, // "zh" for Chinese, "en" for English
    pub lines: Vec<HocrLine>,
}

/// hOCR line structure
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct HocrLine {
    pub id: String,
    pub bbox: (f32, f32, f32, f32), // x0, y0, x1, y1
    pub words: Vec<HocrWord>,
}

/// hOCR word structure
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct HocrWord {
    pub id: String,
    pub bbox: (f32, f32, f32, f32), // x0, y0, x1, y1
    pub text: String,
    pub confidence: f32, // 0.0 to 1.0
}

/// hOCR document generator
pub struct HocrGenerator {
    page_counter: u32,
    paragraph_counter: u32,
    line_counter: u32,
    word_counter: u32,
}

impl HocrGenerator {
    pub fn new() -> Self {
        Self {
            page_counter: 0,
            paragraph_counter: 0,
            line_counter: 0,
            word_counter: 0,
        }
    }
    
    /// Generate hOCR markup from formatted paragraphs
    pub fn generate_hocr(&mut self, paragraphs: &[FormattedParagraph]) -> Result<HocrPage> {
        self.page_counter += 1;
        
        let mut hocr_paragraphs = Vec::new();
        
        for (para_index, paragraph) in paragraphs.iter().enumerate() {
            let hocr_paragraph = self.convert_paragraph(paragraph, para_index)?;
            hocr_paragraphs.push(hocr_paragraph);
        }
        
        // Calculate page bounding box
        let page_bbox = self.calculate_page_bbox(&hocr_paragraphs);
        
        Ok(HocrPage {
            page_number: self.page_counter,
            bbox: page_bbox,
            paragraphs: hocr_paragraphs,
        })
    }
    
    /// Convert a formatted paragraph to hOCR paragraph
    fn convert_paragraph(&mut self, paragraph: &FormattedParagraph, para_index: usize) -> Result<HocrParagraph> {
        self.paragraph_counter += 1;
        let para_id = format!("para_{}", self.paragraph_counter);
        
        let language = if paragraph.is_chinese { "zh" } else { "en" };
        
        let mut hocr_lines = Vec::new();
        
        for (line_index, line) in paragraph.lines.iter().enumerate() {
            let hocr_line = self.convert_line(line, para_index, line_index)?;
            hocr_lines.push(hocr_line);
        }
        
        let bbox = self.calculate_paragraph_bbox(&hocr_lines);
        
        Ok(HocrParagraph {
            id: para_id,
            bbox,
            language: language.to_string(),
            lines: hocr_lines,
        })
    }
    
    /// Convert a formatted line to hOCR line
    fn convert_line(&mut self, line: &FormattedLine, para_index: usize, line_index: usize) -> Result<HocrLine> {
        self.line_counter += 1;
        let line_id = format!("line_{}_{}", para_index, line_index);
        
        let words = self.extract_words_from_line(line)?;
        
        let bbox = self.calculate_line_bbox(&words);
        
        Ok(HocrLine {
            id: line_id,
            bbox,
            words,
        })
    }
    
    /// Extract words from a formatted line
    fn extract_words_from_line(&mut self, line: &FormattedLine) -> Result<Vec<HocrWord>> {
        let mut words = Vec::new();
        
        if line.is_chinese {
            // For Chinese, each character is a "word"
            let mut x_offset = line.x;
            let char_height = line.height;
            
            for ch in line.text.chars() {
                self.word_counter += 1;
                let word_id = format!("word_{}", self.word_counter);
                
                // Estimate character width (this would be more accurate with font metrics)
                let char_width = char_height * 0.8; // Rough estimate
                
                let word = HocrWord {
                    id: word_id,
                    bbox: (x_offset, line.y, x_offset + char_width, line.y + char_height),
                    text: ch.to_string(),
                    confidence: 0.95, // High confidence for rendered text
                };
                
                words.push(word);
                x_offset += char_width;
            }
        } else {
            // For English, split by whitespace
            let mut x_offset = line.x;
            let word_texts: Vec<&str> = line.text.split_whitespace().collect();
            
            for word_text in word_texts {
                self.word_counter += 1;
                let word_id = format!("word_{}", self.word_counter);
                
                // Estimate word width (this would be more accurate with font metrics)
                let word_width = word_text.len() as f32 * line.font_size * 0.6; // Rough estimate
                
                let word = HocrWord {
                    id: word_id,
                    bbox: (x_offset, line.y, x_offset + word_width, line.y + line.height),
                    text: word_text.to_string(),
                    confidence: 0.95,
                };
                
                words.push(word);
                x_offset += word_width + line.font_size * 0.3; // Add space between words
            }
        }
        
        Ok(words)
    }
    
    /// Calculate bounding box for a paragraph
    fn calculate_paragraph_bbox(&self, lines: &[HocrLine]) -> (f32, f32, f32, f32) {
        if lines.is_empty() {
            return (0.0, 0.0, 0.0, 0.0);
        }
        
        let mut min_x = f32::INFINITY;
        let mut min_y = f32::INFINITY;
        let mut max_x = f32::NEG_INFINITY;
        let mut max_y = f32::NEG_INFINITY;
        
        for line in lines {
            let (x0, y0, x1, y1) = line.bbox;
            min_x = min_x.min(x0);
            min_y = min_y.min(y0);
            max_x = max_x.max(x1);
            max_y = max_y.max(y1);
        }
        
        (min_x, min_y, max_x, max_y)
    }
    
    /// Calculate bounding box for a line
    fn calculate_line_bbox(&self, words: &[HocrWord]) -> (f32, f32, f32, f32) {
        if words.is_empty() {
            return (0.0, 0.0, 0.0, 0.0);
        }
        
        let mut min_x = f32::INFINITY;
        let mut min_y = f32::INFINITY;
        let mut max_x = f32::NEG_INFINITY;
        let mut max_y = f32::NEG_INFINITY;
        
        for word in words {
            let (x0, y0, x1, y1) = word.bbox;
            min_x = min_x.min(x0);
            min_y = min_y.min(y0);
            max_x = max_x.max(x1);
            max_y = max_y.max(y1);
        }
        
        (min_x, min_y, max_x, max_y)
    }
    
    /// Calculate bounding box for the entire page
    fn calculate_page_bbox(&self, paragraphs: &[HocrParagraph]) -> (f32, f32, f32, f32) {
        if paragraphs.is_empty() {
            return (0.0, 0.0, 0.0, 0.0);
        }
        
        let mut min_x = f32::INFINITY;
        let mut min_y = f32::INFINITY;
        let mut max_x = f32::NEG_INFINITY;
        let mut max_y = f32::NEG_INFINITY;
        
        for paragraph in paragraphs {
            let (x0, y0, x1, y1) = paragraph.bbox;
            min_x = min_x.min(x0);
            min_y = min_y.min(y0);
            max_x = max_x.max(x1);
            max_y = max_y.max(y1);
        }
        
        (min_x, min_y, max_x, max_y)
    }
    
    /// Generate hOCR HTML markup
    pub fn generate_hocr_html(&self, pages: &[HocrPage]) -> Result<String> {
        let mut html = String::new();
        
        // HTML header
        html.push_str(r#"<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en">
<head>
<meta http-equiv="Content-Type" content="text/html;charset=utf-8" />
<meta name="ocr-system" content="cbeta-pdf-creator 1.0" />
<meta name="ocr-capabilities" content="ocr_page ocr_carea ocr_par ocr_line ocr_word" />
<title>OCR Output</title>
</head>
<body>
"#);
        
        for page in pages {
            html.push_str(&self.generate_page_html(page));
        }
        
        // HTML footer
        html.push_str("</body>\n</html>");
        
        Ok(html)
    }
    
    /// Generate HTML for a single page
    fn generate_page_html(&self, page: &HocrPage) -> String {
        let mut html = String::new();
        
        let (x0, y0, x1, y1) = page.bbox;
        html.push_str(&format!(
            r#"<div class='ocr_page' id='page_{}' title='bbox {} {} {} {}'>"#,
            page.page_number, x0, y0, x1, y1
        ));
        html.push('\n');
        
        for paragraph in &page.paragraphs {
            html.push_str(&self.generate_paragraph_html(paragraph));
        }
        
        html.push_str("</div>\n");
        
        html
    }
    
    /// Generate HTML for a paragraph
    fn generate_paragraph_html(&self, paragraph: &HocrParagraph) -> String {
        let mut html = String::new();
        
        let (x0, y0, x1, y1) = paragraph.bbox;
        html.push_str(&format!(
            r#"  <div class='ocr_par' id='{}' title='bbox {} {} {} {}; lang {}'>"#,
            paragraph.id, x0, y0, x1, y1, paragraph.language
        ));
        html.push('\n');
        
        for line in &paragraph.lines {
            html.push_str(&self.generate_line_html(line));
        }
        
        html.push_str("  </div>\n");
        
        html
    }
    
    /// Generate HTML for a line
    fn generate_line_html(&self, line: &HocrLine) -> String {
        let mut html = String::new();
        
        let (x0, y0, x1, y1) = line.bbox;
        html.push_str(&format!(
            r#"    <span class='ocr_line' id='{}' title='bbox {} {} {} {}'>"#,
            line.id, x0, y0, x1, y1
        ));
        
        for word in &line.words {
            html.push_str(&self.generate_word_html(word));
        }
        
        html.push_str("</span>\n");
        
        html
    }
    
    /// Generate HTML for a word
    fn generate_word_html(&self, word: &HocrWord) -> String {
        let (x0, y0, x1, y1) = word.bbox;
        format!(
            r#"<span class='ocrx_word' id='{}' title='bbox {} {} {} {}; {}'>{}</span>"#,
            word.id, x0, y0, x1, y1, word.confidence, word.text
        )
    }
}

impl Default for HocrGenerator {
    fn default() -> Self {
        Self::new()
    }
}

/// Create an hOCR generator
pub fn create_hocr_generator() -> HocrGenerator {
    HocrGenerator::new()
}

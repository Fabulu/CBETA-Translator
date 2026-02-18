//! Font management for high-quality typography
//! 
//! Provides font loading, caching, and metrics for both Chinese and English text
//! using fontdue for professional rendering.

use anyhow::{Result, anyhow};
use fontdue::{Font, FontSettings};
use std::collections::HashMap;
use std::path::Path;

/// Text justification options
#[derive(Clone, Copy, PartialEq)]
pub enum Justification {
    Left,
    Justify, // English only for now
}

/// Font context containing loaded fonts and rendering settings
#[derive(Clone)]
pub struct FontContext {
    // Chinese fonts (Traditional Chinese)
    pub chinese_font: Font,
    pub chinese_font_name: String,  // Track which font was loaded
    pub chinese_font_path: String,  // Track source path to decide embedding strategy
    pub chinese_font_data: Vec<u8>, // Store raw font data for embedding
    pub chinese_font_bold: Option<Font>,
    
    // English fonts
    pub english_font: Font,
    pub english_font_name: String,  // Track which font was loaded
    pub english_font_path: String,  // Track source path to decide embedding strategy
    pub english_font_data: Vec<u8>, // Store raw font data for embedding
    pub english_font_italic: Option<Font>,
    pub english_font_bold: Option<Font>,
    
    // Layout settings
    pub page_width: f32,
    pub page_height: f32,
    pub margin: f32,
    pub font_size_chinese: f32,
    pub font_size_english: f32,
    pub line_spacing: f32,
    
    // Typography settings for professional print quality
    pub tracking_chinese: f32,   // in 1/1000 em (0 = none, 10-30 = classic print)
    pub tracking_english: f32,
    pub justification: Justification,
    pub paragraph_spacing: f32,  // multiplier of line height (0.4-0.8 typical for books)
    
    // Font metrics cache
    char_metrics: HashMap<char, fontdue::Metrics>,
}

impl FontContext {
    /// Initialize fonts with professional typography choices
    pub fn initialize_fonts() -> Result<Self> {
        // Try to load high-quality fonts, fallback to system fonts
        let (chinese_font, chinese_font_name, chinese_font_path, chinese_font_data) =
            Self::load_chinese_font()?;
        let (english_font, english_font_name, english_font_path, english_font_data) = Self::load_english_font()?;
        
        println!("Loaded fonts: Chinese={}, English={}", chinese_font_name, english_font_name);
        
        Ok(FontContext {
            chinese_font,
            chinese_font_name,
            chinese_font_path,
            chinese_font_data,
            chinese_font_bold: None, // TODO: Load bold variant
            english_font,
            english_font_name,
            english_font_path,
            english_font_data,
            english_font_italic: None, // TODO: Load italic variant
            english_font_bold: None, // TODO: Load bold variant
            
            // Default page settings (A4-like)
            page_width: 595.0, // A4 width in points
            page_height: 842.0, // A4 height in points
            margin: 72.0, // 1 inch margins
            font_size_chinese: 13.0,
            font_size_english: 12.0,
            line_spacing: 1.4,
            
            // Professional typography defaults
            tracking_chinese: 12.0,   // subtle classic Chinese book spacing
            tracking_english: 8.0,    // Garamond/Georgia loves a little air
            justification: Justification::Justify,
            paragraph_spacing: 0.6,
            
            char_metrics: HashMap::new(),
        })
    }
    
    /// Load a high-quality Chinese font
    fn load_chinese_font() -> Result<(Font, String, String, Vec<u8>)> {
        // Priority order for Chinese fonts
        let font_paths = vec![
            // Bundled open-source, print-ready default for Linux/portable builds.
            ("Noto Serif CJK TC", concat!(env!("CARGO_MANIFEST_DIR"), "/assets/fonts/NotoSerifCJKtc-Regular.otf")),
            // Source Han (same glyph design family; also open-source).
            ("Source Han Serif TC", concat!(env!("CARGO_MANIFEST_DIR"), "/assets/fonts/SourceHanSerifTC-Regular.otf")),
            // SimSun (high compatibility for Windows PDF viewers)
            ("SimSun", "C:\\Windows\\Fonts\\simsun.ttc"),
            // Microsoft JhengHei (Windows system font) - Regular
            ("Microsoft JhengHei", "C:\\Windows\\Fonts\\msjh.ttc"),
            // Microsoft YaHei (Windows system font) - Regular  
            ("Microsoft YaHei", "C:\\Windows\\Fonts\\msyh.ttc"),
            // Linux packaged fonts (if installed)
            ("Noto Serif CJK TC", "/usr/share/fonts/opentype/noto/NotoSerifCJK-Regular.ttc"),
            ("Noto Serif CJK TC", "/usr/share/fonts/opentype/noto/NotoSerifCJKtc-Regular.otf"),
            // SimSun Bold TTF (fontdue lookup currently returns missing-glyph for many CJK chars)
            ("SimSun Bold", "C:\\Windows\\Fonts\\simsunb.ttf"),
        ];
        
        for (font_name, font_path) in font_paths {
            if Path::new(font_path).exists() {
                println!("Loading Chinese font: {} from {}", font_name, font_path);
                let font_data = std::fs::read(font_path)?;
                let font = Font::from_bytes(font_data.clone(), FontSettings::default())
                    .map_err(|e| anyhow!("Failed to load Chinese font from {}: {}", font_path, e))?;
                return Ok((
                    font,
                    font_name.to_string(),
                    font_path.to_string(),
                    font_data,
                ));
            }
        }
        
        Err(anyhow!("No suitable Chinese font found"))
    }
    
    /// Load a high-quality English font
    fn load_english_font() -> Result<(Font, String, String, Vec<u8>)> {
        // Priority order for English fonts
        let font_paths = vec![
            // Bundled open-source serif for print-ready portable PDFs.
            ("EB Garamond", concat!(env!("CARGO_MANIFEST_DIR"), "/assets/fonts/EBGaramond-Regular.ttf")),
            ("Noto Serif", concat!(env!("CARGO_MANIFEST_DIR"), "/assets/fonts/NotoSerif-Regular.ttf")),
            // Linux packaged open-source serif fallbacks.
            ("Noto Serif", "/usr/share/fonts/truetype/noto/NotoSerif-Regular.ttf"),
            ("Liberation Serif", "/usr/share/fonts/truetype/liberation2/LiberationSerif-Regular.ttf"),
            // Windows system serif options.
            ("Garamond", "C:\\Windows\\Fonts\\GARABD.TTF"),
            ("Georgia", "C:\\Windows\\Fonts\\georgia.ttf"),
            // Times New Roman (fallback)
            ("Times New Roman", "C:\\Windows\\Fonts\\times.ttf"),
            // Last-resort Linux serif
            ("DejaVu Serif", "/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf"),
        ];
        
        for (font_name, font_path) in font_paths {
            if Path::new(font_path).exists() {
                println!("Loading English font: {} from {}", font_name, font_path);
                let font_data = std::fs::read(font_path)?;
                let font = Font::from_bytes(font_data.clone(), FontSettings::default())
                    .map_err(|e| anyhow!("Failed to load English font from {}: {}", font_path, e))?;
                return Ok((font, font_name.to_string(), font_path.to_string(), font_data));
            }
        }
        
        Err(anyhow!("No suitable English font found"))
    }
    
    /// Set PDF generation options
    pub fn set_options(
        &mut self,
        page_width: f32,
        page_height: f32,
        margin: f32,
        font_size_chinese: f32,
        font_size_english: f32,
        line_spacing: f32,
        tracking_chinese: f32,
        tracking_english: f32,
        paragraph_spacing: f32,
    ) {
        self.page_width = page_width;
        self.page_height = page_height;
        self.margin = margin;
        self.font_size_chinese = font_size_chinese;
        self.font_size_english = font_size_english;
        self.line_spacing = line_spacing;
        self.tracking_chinese = tracking_chinese;
        self.tracking_english = tracking_english;
        self.paragraph_spacing = paragraph_spacing;
    }
    
    /// Get font metrics for a character, with caching
    pub fn get_char_metrics(&mut self, ch: char, is_chinese: bool) -> &fontdue::Metrics {
        let font = if is_chinese { &self.chinese_font } else { &self.english_font };
        let font_size = if is_chinese { self.font_size_chinese } else { self.font_size_english };
        
        if !self.char_metrics.contains_key(&ch) {
            let metrics = font.metrics(ch, font_size);
            self.char_metrics.insert(ch, metrics);
        }
        
        &self.char_metrics[&ch]
    }
    
    /// NEW: Accurate width with kerning + tracking
    pub fn calculate_text_width(&mut self, text: &str, is_chinese: bool) -> f32 {
        let font = if is_chinese { &self.chinese_font } else { &self.english_font };
        let size = if is_chinese { self.font_size_chinese } else { self.font_size_english };
        let tracking = if is_chinese { self.tracking_chinese } else { self.tracking_english };
        let scale = size / font.units_per_em() as f32;

        let chars: Vec<char> = text.chars().collect();
        if chars.is_empty() { return 0.0; }

        let mut width = 0.0;
        for (i, &ch) in chars.iter().enumerate() {
            // Get metrics without borrowing issues
            let font_for_metrics = if is_chinese { &self.chinese_font } else { &self.english_font };
            let font_size_for_metrics = if is_chinese { self.font_size_chinese } else { self.font_size_english };
            
            if !self.char_metrics.contains_key(&ch) {
                let metrics = font_for_metrics.metrics(ch, font_size_for_metrics);
                self.char_metrics.insert(ch, metrics);
            }
            
            let metrics = &self.char_metrics[&ch];
            width += metrics.advance_width;

            if i < chars.len() - 1 {
                // Kerning (fontdue gives it in font units)
                if let Some(kern) = font.horizontal_kern(ch, chars[i + 1], size) {
                    width += kern * scale;
                }
                // Tracking (classic print-book value)
                width += (tracking / 1000.0) * size;
            }
        }
        width
    }
    
    /// Get line height based on font size and line spacing
    pub fn get_line_height(&self, is_chinese: bool) -> f32 {
        let font_size = if is_chinese { self.font_size_chinese } else { self.font_size_english };
        font_size * self.line_spacing
    }
    
    /// Get content area (page minus margins)
    pub fn content_area(&self) -> (f32, f32, f32, f32) {
        (
            self.margin, // left
            self.margin, // top
            self.page_width - 2.0 * self.margin, // width
            self.page_height - 2.0 * self.margin, // height
        )
    }
}

/// Initialize fonts for the PDF creator
pub fn initialize_fonts() -> Result<FontContext> {
    FontContext::initialize_fonts()
}

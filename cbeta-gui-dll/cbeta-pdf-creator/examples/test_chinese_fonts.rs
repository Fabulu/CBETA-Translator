//! Test Chinese font support with multiple font options
//! 
//! This example demonstrates Chinese font rendering using both:
//! 1. Windows system fonts (Microsoft JhengHei, Microsoft YaHei, SimSun)
//! 2. Source Han Sans TC (high-quality Adobe font)

use std::ffi::{CStr, CString};
use std::ptr;
use cbeta_pdf_creator::{fonts::FontContext, create_bilingual_pdf_with_context};

fn main() -> Result<(), Box<dyn std::error::Error>> {
    println!("Testing Chinese Font Support in CBETA PDF Creator...");
    
    // Test data with various Chinese characters to test font coverage
    let test_texts = vec![
        // Basic Buddhist terms
        "佛說阿彌陀經".to_string(),
        "如是我聞。一時佛在舍衛國祇樹給孤獨園。".to_string(),
        
        // Complex characters that test font coverage
        "觀自在菩薩。行深般若波羅蜜多時。照見五蘊皆空。".to_string(),
        
        // Common characters and punctuation
        "舍利子！色不異空，空不異色；色即是空，空即是色。".to_string(),
        
        // Numbers and special characters
        "第一百二十三卷 第四五六七八九頁".to_string(),
        
        // Mixed traditional and simplified
        "眾生無邊誓願度，煩惱無盡誓願斷，法門無量誓願學，佛道無上誓願成。".to_string(),
    ];
    
    let english_translations = vec![
        "The Amitabha Sutra".to_string(),
        "Thus have I heard. At one time the Buddha was staying in the Jeta Grove of Anathapindika's park in Sravasti.".to_string(),
        "Avalokiteshvara Bodhisattva, when practicing the profound Prajnaparamita, illuminated the five skandhas and saw they are empty.".to_string(),
        "Shariputra! Form is not different from emptiness, emptiness is not different from form; form is emptiness, emptiness is form.".to_string(),
        "Volume 123, Pages 456789".to_string(),
        "Sentient beings are numberless, I vow to save them; Desires are inexhaustible, I vow to extinguish them; Dharmas are boundless, I vow to learn them; The Buddha way is unsurpassable, I vow to attain it.".to_string(),
    ];
    
    // Test with default font loading (shows which font gets loaded)
    println!("\n=== TEST 1: Default Font Loading ===");
    test_with_default_font(&test_texts, &english_translations)?;
    
    // Test with specific Source Han Sans font
    println!("\n=== TEST 2: Source Han Sans TC Font ===");
    test_with_source_han_sans(&test_texts, &english_translations)?;
    
    // Test with Microsoft JhengHei font
    println!("\n=== TEST 3: Microsoft JhengHei Font ===");
    test_with_microsoft_jhenghei(&test_texts, &english_translations)?;
    
    // Test with SimSun font
    println!("\n=== TEST 4: SimSun Font ===");
    test_with_simsun(&test_texts, &english_translations)?;
    
    println!("\n✅ All Chinese font tests completed!");
    println!("Check the generated PDF files to verify Chinese character rendering.");
    
    Ok(())
}

/// Test with default font loading (shows priority order)
fn test_with_default_font(
    chinese_texts: &[String],
    english_texts: &[String],
) -> Result<(), Box<dyn std::error::Error>> {
    println!("Loading fonts with default priority order...");
    
    let font_context = FontContext::initialize_fonts()?;
    println!("✅ Loaded Chinese font: {}", font_context.chinese_font_name);
    println!("✅ Loaded English font: {}", font_context.english_font_name);
    
    let output_path = "test_chinese_font_default.pdf";
    create_bilingual_pdf_with_context(chinese_texts, english_texts, output_path, &font_context)?;
    
    println!("✅ Generated: {} (using {} for Chinese)", output_path, font_context.chinese_font_name);
    Ok(())
}

/// Test specifically with Source Han Sans TC font
fn test_with_source_han_sans(
    chinese_texts: &[String],
    english_texts: &[String],
) -> Result<(), Box<dyn std::error::Error>> {
    println!("Attempting to load Source Han Sans TC font...");
    
    let mut font_context = FontContext::initialize_fonts()?;
    
    // Try to load Source Han Sans specifically
    let font_path = "D:\\Rust-projects\\not-rust-projects\\CBETA-GUI\\15_SourceHanSansHWTC\\OTF\\TraditionalChineseHW\\SourceHanSansHWTC-Regular.otf";
    
    if std::path::Path::new(font_path).exists() {
        let font_data = std::fs::read(font_path)?;
        let font = fontdue::Font::from_bytes(font_data, fontdue::FontSettings::default())?;
        font_context.chinese_font = font.clone();
        font_context.chinese_font_name = "Source Han Sans TC Regular".to_string();
        
        println!("✅ Successfully loaded Source Han Sans TC font");
        
        let output_path = "test_chinese_font_source_han_sans.pdf";
        create_bilingual_pdf_with_context(chinese_texts, english_texts, output_path, &font_context)?;
        
        println!("✅ Generated: {} (using Source Han Sans TC for Chinese)", output_path);
    } else {
        println!("❌ Source Han Sans TC font not found at: {}", font_path);
    }
    
    Ok(())
}

/// Test specifically with Microsoft JhengHei font
fn test_with_microsoft_jhenghei(
    chinese_texts: &[String],
    english_texts: &[String],
) -> Result<(), Box<dyn std::error::Error>> {
    println!("Attempting to load Microsoft JhengHei font...");
    
    let mut font_context = FontContext::initialize_fonts()?;
    
    // Try to load Microsoft JhengHei specifically
    let font_path = "C:\\Windows\\Fonts\\msjh.ttc";
    
    if std::path::Path::new(font_path).exists() {
        let font_data = std::fs::read(font_path)?;
        let font = fontdue::Font::from_bytes(font_data, fontdue::FontSettings::default())?;
        font_context.chinese_font = font.clone();
        font_context.chinese_font_name = "Microsoft JhengHei".to_string();
        
        println!("✅ Successfully loaded Microsoft JhengHei font");
        
        let output_path = "test_chinese_font_ms_jhenghei.pdf";
        create_bilingual_pdf_with_context(chinese_texts, english_texts, output_path, &font_context)?;
        
        println!("✅ Generated: {} (using Microsoft JhengHei for Chinese)", output_path);
    } else {
        println!("❌ Microsoft JhengHei font not found at: {}", font_path);
    }
    
    Ok(())
}

/// Test specifically with SimSun font
fn test_with_simsun(
    chinese_texts: &[String],
    english_texts: &[String],
) -> Result<(), Box<dyn std::error::Error>> {
    println!("Attempting to load SimSun font...");
    
    let mut font_context = FontContext::initialize_fonts()?;
    
    // Try to load SimSun specifically
    let font_path = "C:\\Windows\\Fonts\\simsun.ttc";
    
    if std::path::Path::new(font_path).exists() {
        let font_data = std::fs::read(font_path)?;
        let font = fontdue::Font::from_bytes(font_data, fontdue::FontSettings::default())?;
        font_context.chinese_font = font.clone();
        font_context.chinese_font_name = "SimSun".to_string();
        
        println!("✅ Successfully loaded SimSun font");
        
        let output_path = "test_chinese_font_simsun.pdf";
        create_bilingual_pdf_with_context(chinese_texts, english_texts, output_path, &font_context)?;
        
        println!("✅ Generated: {} (using SimSun for Chinese)", output_path);
    } else {
        println!("❌ SimSun font not found at: {}", font_path);
    }
    
    Ok(())
}

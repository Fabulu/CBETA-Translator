//! Direct font comparison test
//! 
//! Creates a single PDF showing Chinese text rendered with different fonts
//! with clear English labels identifying each font.

use cbeta_pdf_creator::{fonts::FontContext, create_bilingual_pdf_with_context};

fn main() -> Result<(), Box<dyn std::error::Error>> {
    println!("Creating Chinese Font Comparison PDF...");
    
    // Test text with various Chinese characters
    let chinese_texts = vec![
        "=== FONT COMPARISON TEST ===".to_string(),
        "Source Han Sans TC Regular: 佛說阿彌陀經，如是我聞。一時佛在舍衛國祇樹給孤獨園。".to_string(),
        "Microsoft JhengHei: 佛說阿彌陀經，如是我聞。一時佛在舍衛國祇樹給孤獨園。".to_string(),
        "Microsoft YaHei: 佛說阿彌陀經，如是我聞。一時佛在舍衛國祇樹給孤獨園。".to_string(),
        "SimSun: 佛說阿彌陀經，如是我聞。一時佛在舍衛國祇樹給孤獨園。".to_string(),
        "".to_string(), // Empty line for spacing
        "Complex Characters Test:".to_string(),
        "觀自在菩薩。行深般若波羅蜜多時。照見五蘊皆空。".to_string(),
        "舍利子！色不異空，空不異色；色即是空，空即是色。".to_string(),
        "".to_string(), // Empty line for spacing
        "Numbers and Punctuation:".to_string(),
        "第一百二十三卷 第四五六七八九頁 【經文】".to_string(),
    ];
    
    let english_texts = vec![
        "=== CHINESE FONT RENDERING COMPARISON ===".to_string(),
        "High-quality Adobe font with excellent character coverage".to_string(),
        "Windows system font, good for Traditional Chinese".to_string(),
        "Windows system font, good for Simplified Chinese".to_string(),
        "Classic Windows serif font, traditional style".to_string(),
        "".to_string(),
        "Testing complex character rendering:".to_string(),
        "Heart Sutra - testing character coverage and quality".to_string(),
        "Testing punctuation and special characters".to_string(),
        "".to_string(),
        "Testing numbers and brackets:".to_string(),
        "Volume 123, Pages 456789 with brackets".to_string(),
    ];
    
    // Test with default (Source Han Sans should be loaded first)
    let font_context = FontContext::initialize_fonts()?;
    println!("Loaded Chinese font: {}", font_context.chinese_font_name);
    
    let output_path = "chinese_font_comparison.pdf";
    create_bilingual_pdf_with_context(&chinese_texts, &english_texts, output_path, &font_context)?;
    
    println!("✅ Generated: {}", output_path);
    println!("This PDF shows Chinese text rendering with the {} font.", font_context.chinese_font_name);
    println!("Each line is labeled to show which font is being used for comparison.");
    
    Ok(())
}

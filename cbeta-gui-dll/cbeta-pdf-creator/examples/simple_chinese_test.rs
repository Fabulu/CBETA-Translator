//! Simple Chinese font test
//! 
//! Tests basic Chinese character rendering with a simple approach.

use cbeta_pdf_creator::{fonts::FontContext, create_bilingual_pdf_with_context};

fn main() -> Result<(), Box<dyn std::error::Error>> {
    println!("Testing Simple Chinese Character Rendering...");
    
    // Very simple test text
    let chinese_texts = vec![
        "佛".to_string(),
        "說".to_string(), 
        "阿".to_string(),
        "彌".to_string(),
        "陀".to_string(),
        "經".to_string(),
    ];
    
    let english_texts = vec![
        "Buddha".to_string(),
        "Speaks".to_string(),
        "A".to_string(),
        "Mi".to_string(),
        "Ta".to_string(),
        "Sutra".to_string(),
    ];
    
    // Load fonts
    let font_context = FontContext::initialize_fonts()?;
    println!("Using Chinese font: {}", font_context.chinese_font_name);
    println!("Font data size: {} bytes", font_context.chinese_font_data.len());
    
    // Create PDF
    let output_path = "simple_chinese_test.pdf";
    create_bilingual_pdf_with_context(&chinese_texts, &english_texts, output_path, &font_context)?;
    
    println!("✅ Generated: {}", output_path);
    println!("This PDF should show individual Chinese characters.");
    
    Ok(())
}

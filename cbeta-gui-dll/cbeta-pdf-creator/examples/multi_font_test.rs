//! Multi-font test that creates separate PDFs for each Chinese font
//! 
//! This test creates individual PDFs using different Chinese fonts to clearly
//! demonstrate the rendering differences between Windows system fonts and Source Han Sans.

use cbeta_pdf_creator::{fonts::FontContext, create_bilingual_pdf_with_context};
use fontdue::Font;

fn main() -> Result<(), Box<dyn std::error::Error>> {
    println!("Creating Multi-Font Chinese PDF Comparison...");
    
    // Test text with comprehensive character coverage
    let test_chinese = vec![
        "Chinese Font Rendering Test".to_string(),
        "佛說阿彌陀經".to_string(),
        "如是我聞。一時佛在舍衛國祇樹給孤獨園。".to_string(),
        "與大比丘僧，千二百五十人俱，皆是大阿羅漢。".to_string(),
        "觀自在菩薩。行深般若波羅蜜多時。照見五蘊皆空。".to_string(),
        "舍利子！色不異空，空不異色；色即是空，空即是色。".to_string(),
        "受想行識，亦復如是。".to_string(),
        "眾生無邊誓願度，煩惱無盡誓願斷。".to_string(),
        "法門無量誓願學，佛道無上誓願成。".to_string(),
    ];
    
    let test_english = vec![
        "Font Rendering Quality Comparison".to_string(),
        "The Amitabha Sutra".to_string(),
        "Thus have I heard. At one time the Buddha was staying in the Jeta Grove of Anathapindika's park in Sravasti.".to_string(),
        "Together with a great assembly of twelve hundred and fifty monks, all of whom were great Arhats.".to_string(),
        "Avalokiteshvara Bodhisattva, when practicing the profound Prajnaparamita, illuminated the five skandhas and saw they are empty.".to_string(),
        "Shariputra! Form is not different from emptiness, emptiness is not different from form; form is emptiness, emptiness is form.".to_string(),
        "Feelings, perceptions, mental formations, and consciousness are also like this.".to_string(),
        "Sentient beings are numberless, I vow to save them; Desires are inexhaustible, I vow to extinguish them.".to_string(),
        "Dharmas are boundless, I vow to learn them; The Buddha way is unsurpassable, I vow to attain it.".to_string(),
    ];
    
    // Test 1: Source Han Sans TC (highest quality)
    test_font("Source Han Sans TC", 
              "D:\\Rust-projects\\not-rust-projects\\CBETA-GUI\\15_SourceHanSansHWTC\\OTF\\TraditionalChineseHW\\SourceHanSansHWTC-Regular.otf",
              &test_chinese, &test_english, 
              "test_source_han_sans_tc.pdf")?;
    
    // Test 2: Microsoft JhengHei (Windows Traditional Chinese)
    test_font("Microsoft JhengHei", 
              "C:\\Windows\\Fonts\\msjh.ttc",
              &test_chinese, &test_english, 
              "test_microsoft_jhenghei.pdf")?;
    
    // Test 3: Microsoft YaHei (Windows Simplified Chinese)
    test_font("Microsoft YaHei", 
              "C:\\Windows\\Fonts\\msyh.ttc",
              &test_chinese, &test_english, 
              "test_microsoft_yahei.pdf")?;
    
    // Test 4: SimSun (Classic Windows)
    test_font("SimSun", 
              "C:\\Windows\\Fonts\\simsun.ttc",
              &test_chinese, &test_english, 
              "test_simsun.pdf")?;
    
    println!("\n✅ Multi-font test completed!");
    println!("Generated PDFs:");
    println!("  - test_source_han_sans_tc.pdf (Adobe Source Han Sans TC)");
    println!("  - test_microsoft_jhenghei.pdf (Microsoft JhengHei)");
    println!("  - test_microsoft_yahei.pdf (Microsoft YaHei)");
    println!("  - test_simsun.pdf (SimSun)");
    println!("\nCompare these PDFs to see the rendering quality differences between fonts.");
    
    Ok(())
}

fn test_font(
    font_name: &str,
    font_path: &str,
    chinese_texts: &[String],
    english_texts: &[String],
    output_filename: &str,
) -> Result<(), Box<dyn std::error::Error>> {
    println!("\n--- Testing {} ---", font_name);
    
    if !std::path::Path::new(font_path).exists() {
        println!("❌ Font not found: {}", font_path);
        return Ok(());
    }
    
    // Load the specific font
    let font_data = std::fs::read(font_path)?;
    let chinese_font = Font::from_bytes(font_data, fontdue::FontSettings::default())?;
    
    // Create font context with the specific Chinese font
    let mut font_context = FontContext::initialize_fonts()?;
    font_context.chinese_font = chinese_font;
    font_context.chinese_font_name = font_name.to_string();
    
    println!("✅ Loaded font: {}", font_name);
    
    // Create PDF with this font
    create_bilingual_pdf_with_context(chinese_texts, english_texts, output_filename, &font_context)?;
    
    println!("✅ Generated: {} (using {})", output_filename, font_name);
    
    Ok(())
}

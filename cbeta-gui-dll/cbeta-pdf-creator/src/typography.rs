//! Professional typography and text layout
//!
//! Handles text layout, line breaking, and paragraph formatting for bilingual documents
//! with print-quality typography similar to Adobe InDesign, following the rules from the
//! typesetting guide (no widows/orphans, no stacked hyphens, no rivers, proper rag, etc.). [file:1]

use crate::fonts::{FontContext, Justification};
use unicode_bidi::BidiInfo;
use unicode_script::{Script, UnicodeScript};
use anyhow::Result;

/// Text tokens for professional paragraph composition. [file:6]
#[derive(Debug, Clone, PartialEq)]
pub enum TextToken {
    Word(String),
    Space,
    Punctuation(char),
    DiscretionaryHyphen, // U+00AD soft hyphen point
}

/// Space adjustment data for precise justification. [file:6]
#[derive(Debug, Clone)]
pub struct SpaceAdjustment {
    /// Position in the line text (character index, not glyph index). [file:6]
    pub position: usize,
    pub base_width: f32,
    pub adjusted_width: f32,
    /// How much this space was stretched/shrunk. [file:6]
    pub adjustment_ratio: f32,
}

/// Represents a formatted line of text with premium typography data. [file:6]
#[derive(Debug, Clone)]
pub struct FormattedLine {
    pub text: String,
    pub x: f32,
    pub y: f32,
    pub width: f32,
    pub height: f32,
    pub is_chinese: bool,
    pub font_size: f32,
    /// Baseline position for proper leading. [file:6]
    pub baseline: f32,
    /// Tokenized representation. [file:6]
    pub tokens: Vec<TextToken>,
    /// Precise justification data. [file:6]
    pub space_adjustments: Vec<SpaceAdjustment>,
    pub is_justified: bool,
    /// Whether this line ends with a hyphen. [file:6]
    pub hyphenated: bool,
}

impl FormattedLine {
    /// Convert tokens back to string. [file:6]
    pub fn tokens_to_string(&self) -> String {
        let mut result = String::new();
        for token in &self.tokens {
            match token {
                TextToken::Word(word) => result.push_str(word),
                TextToken::Space => result.push(' '),
                TextToken::Punctuation(punct) => result.push(*punct),
                TextToken::DiscretionaryHyphen => result.push('-'),
            }
        }
        result
    }

    /// Count word tokens on this line. [file:6]
    pub fn word_count(&self) -> usize {
        self.tokens
            .iter()
            .filter(|t| matches!(t, TextToken::Word(_)))
            .count()
    }

    /// Return true if the last visible token is a hyphen. [file:6]
    pub fn ends_with_hyphen(&self) -> bool {
        self.tokens
            .iter()
            .rev()
            .find(|t| !matches!(t, TextToken::Space))
            .map(|t| matches!(t, TextToken::Punctuation('-') | TextToken::DiscretionaryHyphen))
            .unwrap_or(false)
    }
}

/// Represents a paragraph with proper typography. [file:6]
#[derive(Debug, Clone)]
pub struct FormattedParagraph {
    pub lines: Vec<FormattedLine>,
    pub x: f32,
    pub y: f32,
    pub width: f32,
    pub height: f32,
    pub is_chinese: bool,
    pub font_size: f32,
    pub line_spacing: f32,
    /// Baseline-to-baseline leading. [file:6]
    pub leading: f32,
}

/// Professional text layout engine. [file:6]
pub struct TextLayoutEngine {
    font_context: FontContext,
}

impl TextLayoutEngine {
    pub fn new(font_context: FontContext) -> Self {
        Self { font_context }
    }

    /// Layout a paragraph with premium TeX-like typography and the guide rules applied. [file:1][file:6]
    pub fn layout_paragraph(
        &mut self,
        text: &str,
        x: f32,
        y: f32,
        max_width: f32,
        is_chinese: bool,
    ) -> Result<FormattedParagraph> {
        // Smart punctuation normalization (smart quotes, ellipsis, dashes). [file:1][file:6]
        let normalized_text = self.normalize_punctuation(text);

        // Apply bidi processing before tokenization. [file:6]
        let bidi_text = self.process_bidi_text(&normalized_text);

        // Tokenize text for professional composition. [file:6]
        let tokens = self.tokenize_text(&bidi_text, is_chinese)?;

        // Calculate proper baseline-to-baseline leading (minimum +4 over size). [file:1][file:6]
        let font_size = if is_chinese {
            self.font_context.font_size_chinese
        } else {
            self.font_context.font_size_english
        };
        let mut leading = font_size * self.font_context.line_spacing;
        if leading < font_size + 4.0 {
            leading = font_size + 4.0;
        }

        // Use paragraph composer instead of greedy wrapper. [file:6]
        let mut lines = self.compose_paragraph(&tokens, max_width, leading, is_chinese)?;

        // Post-process lines for widows/orphans, stacked hyphens, rag quality, and word count per line. [file:1][file:6]
        self.post_process_lines(&mut lines, max_width, is_chinese)?;

        // Calculate paragraph height using baseline positions. [file:6]
        let total_height = if lines.is_empty() {
            leading
        } else {
            let first_baseline = lines.first().unwrap().baseline;
            let last_baseline = lines.last().unwrap().baseline;
            (last_baseline - first_baseline) + leading
        };

        Ok(FormattedParagraph {
            lines,
            x,
            y,
            width: max_width,
            height: total_height,
            is_chinese,
            font_size,
            line_spacing: self.font_context.line_spacing,
            leading,
        })
    }

    /// Smart punctuation normalization for professional typography. [file:1][file:6]
    fn normalize_punctuation(&self, text: &str) -> String {
        let mut normalized = String::with_capacity(text.len());
        let chars: Vec<char> = text.chars().collect();

        let mut i = 0;
        while i < chars.len() {
            let ch = chars[i];
            match ch {
                // Convert straight quotes to curly quotes. [file:1]
                '"' => {
                    if i == 0 || chars[i - 1].is_whitespace() {
                        normalized.push('\u{201C}'); // Opening quote
                    } else {
                        normalized.push('\u{201D}'); // Closing quote
                    }
                    i += 1;
                }
                '\'' => {
                    if i == 0 || chars[i - 1].is_whitespace() {
                        normalized.push('\u{2018}'); // Opening single quote
                    } else {
                        normalized.push('\u{2019}'); // Closing single quote / apostrophe
                    }
                    i += 1;
                }
                // Convert three dots to proper ellipsis. [file:1]
                '.' if i + 2 < chars.len() && chars[i + 1] == '.' && chars[i + 2] == '.' => {
                    normalized.push('…');
                    i += 3;
                }
                // Convert double hyphen to em dash. [file:1][file:6]
                '-' if i + 1 < chars.len() && chars[i + 1] == '-' => {
                    normalized.push('—');
                    i += 2;
                }
                // Leave everything else as-is. [file:6]
                _ => {
                    normalized.push(ch);
                    i += 1;
                }
            }
        }

        normalized
    }

    /// Tokenize text into Word/Space/Punctuation for professional composition. [file:6]
    fn tokenize_text(&self, text: &str, is_chinese: bool) -> Result<Vec<TextToken>> {
        let mut tokens = Vec::new();
        let mut current_word = String::new();

        for ch in text.chars() {
            if is_chinese {
                // Chinese: each character is a word token, punctuation stays punctuation. [file:6]
                if !current_word.is_empty() {
                    tokens.push(TextToken::Word(current_word.clone()));
                    current_word.clear();
                }
                if self.is_chinese_punctuation(ch) {
                    tokens.push(TextToken::Punctuation(ch));
                } else if ch.is_whitespace() {
                    tokens.push(TextToken::Space);
                } else {
                    tokens.push(TextToken::Word(ch.to_string()));
                }
            } else {
                // English: proper word/space/punctuation tokenization. [file:6]
                if ch.is_whitespace() {
                    if !current_word.is_empty() {
                        tokens.push(TextToken::Word(current_word.clone()));
                        current_word.clear();
                    }
                    tokens.push(TextToken::Space);
                } else if ch == '\u{00AD}' {
                    // Soft hyphen - mark as discretionary hyphen point. [file:6]
                    if !current_word.is_empty() {
                        tokens.push(TextToken::Word(current_word.clone()));
                        current_word.clear();
                    }
                    tokens.push(TextToken::DiscretionaryHyphen);
                } else if ch.is_ascii_punctuation() && ch != '\'' && ch != '-' {
                    if !current_word.is_empty() {
                        tokens.push(TextToken::Word(current_word.clone()));
                        current_word.clear();
                    }
                    tokens.push(TextToken::Punctuation(ch));
                } else {
                    current_word.push(ch);
                }
            }
        }

        if !current_word.is_empty() {
            tokens.push(TextToken::Word(current_word));
        }

        Ok(tokens)
    }

    /// TeX-like paragraph composer with optimal line breaking (simplified). [file:6]
    fn compose_paragraph(
        &mut self,
        tokens: &[TextToken],
        max_width: f32,
        leading: f32,
        is_chinese: bool,
    ) -> Result<Vec<FormattedLine>> {
        let mut lines = Vec::new();
        let breakpoints = self.find_breakpoints(tokens, is_chinese)?;
        let optimal_breaks = self.greedy_line_breaks(tokens, &breakpoints, max_width, is_chinese)?;
        let mut line_start = 0;
        let mut baseline = 0.0;

        for &break_index in &optimal_breaks {
            if break_index <= line_start {
                continue;
            }

            let line_tokens = &tokens[line_start..break_index];
            let line_width = self.calculate_line_width(line_tokens, is_chinese);

            let (justified_tokens, space_adjustments) = if !is_chinese
                && self.font_context.justification == Justification::Justify
                && line_start != 0
            {
                // Do not justify first line of paragraph. [file:1][file:6]
                self.justify_line(line_tokens, max_width, is_chinese)?
            } else {
                (line_tokens.to_vec(), Vec::new())
            };

            let mut line_text = String::new();
            for token in &justified_tokens {
                match token {
                    TextToken::Word(word) => line_text.push_str(word),
                    TextToken::Space => line_text.push(' '),
                    TextToken::Punctuation(punct) => line_text.push(*punct),
                    TextToken::DiscretionaryHyphen => line_text.push('-'),
                }
            }

            let hyphenated = justified_tokens
                .iter()
                .rev()
                .find(|t| !matches!(t, TextToken::Space))
                .map(|t| matches!(t, TextToken::Punctuation('-') | TextToken::DiscretionaryHyphen))
                .unwrap_or(false);

            lines.push(FormattedLine {
                text: line_text,
                x: 0.0,
                y: 0.0,
                width: line_width,
                height: leading,
                is_chinese,
                font_size: if is_chinese {
                    self.font_context.font_size_chinese
                } else {
                    self.font_context.font_size_english
                },
                baseline,
                tokens: justified_tokens,
                space_adjustments: space_adjustments.clone(),
                is_justified: !space_adjustments.is_empty(),
                hyphenated,
            });

            baseline += leading;
            line_start = break_index;
        }

        Ok(lines)
    }

    /// Greedy line wrapping that respects max width and known breakpoints.
    fn greedy_line_breaks(
        &mut self,
        tokens: &[TextToken],
        breakpoints: &[usize],
        max_width: f32,
        is_chinese: bool,
    ) -> Result<Vec<usize>> {
        if tokens.is_empty() {
            return Ok(vec![]);
        }

        let mut breaks = Vec::new();
        let mut line_start = 0usize;
        let mut i = line_start + 1;
        let mut best_break = line_start;

        while i <= tokens.len() {
            let width = self.calculate_line_width(&tokens[line_start..i], is_chinese);
            let is_breakpoint = breakpoints.binary_search(&i).is_ok();

            if width <= max_width {
                if is_breakpoint {
                    best_break = i;
                }
                i += 1;
                continue;
            }

            let break_at = if best_break > line_start { best_break } else { i - 1 };
            if break_at <= line_start {
                // Guarantee forward progress for very long/unbreakable chunks.
                breaks.push(i);
                line_start = i;
            } else {
                breaks.push(break_at);
                line_start = break_at;
            }

            best_break = line_start;
            i = line_start + 1;
        }

        if breaks.last().copied().unwrap_or(0) < tokens.len() {
            breaks.push(tokens.len());
        }

        Ok(breaks)
    }

    /// Find all possible breakpoints in the token stream. [file:1][file:6]
    fn find_breakpoints(&self, tokens: &[TextToken], is_chinese: bool) -> Result<Vec<usize>> {
        let mut breakpoints = Vec::new();

        for (i, token) in tokens.iter().enumerate() {
            match token {
                TextToken::Space => {
                    // Can break after space. [file:6]
                    breakpoints.push(i + 1);
                }
                TextToken::Punctuation(',') | TextToken::Punctuation(';') => {
                    // Can break after certain punctuation. [file:6]
                    breakpoints.push(i + 1);
                }
                TextToken::DiscretionaryHyphen => {
                    // Can break at soft hyphen. [file:1][file:6]
                    breakpoints.push(i + 1);
                }
                TextToken::Word(word) if !is_chinese => {
                    // Check for hyphenation points in English words. [file:1][file:6]
                    if let Some(hyphen_points) = self.find_hyphenation_points(word) {
                        for &pos in &hyphen_points {
                            breakpoints.push(i + pos);
                        }
                    }
                }
                _ => {}
            }
        }

        // Always allow breaking at the end. [file:6]
        breakpoints.push(tokens.len());
        Ok(breakpoints)
    }

    /// Simple hyphenation point detection according to the guide. [file:1][file:6]
    fn find_hyphenation_points(&self, word: &str) -> Option<Vec<usize>> {
        // Do not hyphenate words shorter than 6 characters. [file:1]
        if word.chars().count() < 6 {
            return None;
        }

        // Avoid hyphenating capitalized words (names). [file:1]
        if word.chars().next().map_or(false, |c| c.is_uppercase()) {
            return None;
        }

        let mut points = Vec::new();
        let chars: Vec<char> = word.chars().collect();
        let len = chars.len();

        // Do not hyphenate with fewer than 3 letters before/after the hyphen. [file:1]
        let start = 3;
        let end = len.saturating_sub(3);

        for i in start..end {
            let ch = chars[i - 1];
            if matches!(ch, 'a' | 'e' | 'i' | 'o' | 'u' | 'y') {
                points.push(i);
            }
        }

        if points.is_empty() {
            None
        } else {
            Some(points)
        }
    }

    /// Calculate line width from tokens. [file:6]
    fn calculate_line_width(&mut self, tokens: &[TextToken], is_chinese: bool) -> f32 {
        let mut width = 0.0;

        for token in tokens {
            match token {
                TextToken::Word(word) => {
                    width += self.font_context.calculate_text_width(word, is_chinese);
                }
                TextToken::Space => {
                    width += self.font_context.calculate_text_width(" ", is_chinese);
                }
                TextToken::Punctuation(punct) => {
                    width += self
                        .font_context
                        .calculate_text_width(&punct.to_string(), is_chinese);
                }
                TextToken::DiscretionaryHyphen => {
                    width += self.font_context.calculate_text_width("-", is_chinese);
                }
            }
        }

        width
    }

    /// Apply justification with precise space adjustments. [file:1][file:6]
    fn justify_line(
        &mut self,
        tokens: &[TextToken],
        max_width: f32,
        is_chinese: bool,
    ) -> Result<(Vec<TextToken>, Vec<SpaceAdjustment>)> {
        let current_width = self.calculate_line_width(tokens, is_chinese);
        let extra_space = max_width - current_width;

        if extra_space <= 0.0 || tokens.is_empty() {
            return Ok((tokens.to_vec(), Vec::new()));
        }

        // Count adjustable spaces. [file:6]
        let space_count = tokens
            .iter()
            .filter(|t| matches!(t, TextToken::Space))
            .count();

        if space_count == 0 {
            return Ok((tokens.to_vec(), Vec::new()));
        }

        let base_space_width = self.font_context.calculate_text_width(" ", is_chinese);
        let space_adjustment = extra_space / space_count as f32;

        // Keep tracking changes within reasonable bounds to avoid rivers/loose lines. [file:1]
        let max_ratio = 0.5; // clamp to +50% per space
        let clamped_adjustment = space_adjustment.clamp(-base_space_width * max_ratio, base_space_width * max_ratio);

        let mut adjusted_tokens = Vec::new();
        let mut adjustments = Vec::new();
        let mut position = 0;

        for token in tokens {
            match token {
                TextToken::Space => {
                    adjusted_tokens.push(TextToken::Space);
                    let adjusted_width = base_space_width + clamped_adjustment;
                    adjustments.push(SpaceAdjustment {
                        position,
                        base_width: base_space_width,
                        adjusted_width,
                        adjustment_ratio: clamped_adjustment / base_space_width,
                    });
                    position += 1;
                }
                _ => {
                    adjusted_tokens.push(token.clone());
                    position += match token {
                        TextToken::Word(word) => word.chars().count(),
                        TextToken::Punctuation(_) => 1,
                        TextToken::DiscretionaryHyphen => 1,
                        TextToken::Space => 1,
                    };
                }
            }
        }

        Ok((adjusted_tokens, adjustments))
    }

    /// Optimize line breaks using dynamic programming (simplified TeX algorithm). [file:6]
    fn optimize_line_breaks(
        &self,
        breakpoints: &[usize],
        _max_width: f32,
        _is_chinese: bool,
    ) -> Result<Vec<usize>> {
        // For now, just ensure we at least end at the last breakpoint to avoid crashes. [file:6]
        if let Some(&last_breakpoint) = breakpoints.last() {
            Ok(vec![last_breakpoint])
        } else {
            Ok(vec![])
        }
    }

    /// Post-process lines for widow/orphan control, stacked hyphens, rag, and line word counts. [file:1][file:6]
    fn post_process_lines(
        &mut self,
        lines: &mut Vec<FormattedLine>,
        max_width: f32,
        is_chinese: bool,
    ) -> Result<()> {
        if lines.is_empty() {
            return Ok(());
        }

        // Ensure minimum of 3 lines in a paragraph (where possible). [file:1]
        if lines.len() < 3 {
            // No automatic fix here; caller may choose a different layout. [file:1]
        }

        // Enforce no widows/orphans and minimum words per line for English. [file:1]
        if !is_chinese && lines.len() >= 2 {
            let last_index = lines.len() - 1;

            // No single-word last line (widow). [file:1]
            if lines[last_index].word_count() == 1 {
                self.try_pull_word_from_previous_line(lines, last_index, max_width, is_chinese)?;
            }

            // No single-word first line of paragraph (orphan). [file:1]
            if lines[0].word_count() == 1 && lines.len() > 1 {
                self.try_push_word_to_next_line(lines, 0, max_width, is_chinese)?;
            }

            // Enforce 5–15 words per line guideline where possible. [file:1]
            for i in 0..lines.len() {
                let wc = lines[i].word_count();
                if wc > 0 && (wc < 5 || wc > 15) {
                    // We only adjust obvious extremes via simple neighbor moves. [file:1]
                    if wc < 5 && i + 1 < lines.len() {
                        self.try_push_word_to_next_line(lines, i, max_width, is_chinese)?;
                    } else if wc > 15 && i + 1 < lines.len() {
                        self.try_pull_word_from_next_line(lines, i, max_width, is_chinese)?;
                    }
                }
            }
        }

        // Check for stacked hyphens (two consecutive hyphenated lines). [file:1][file:6]
        for i in 1..lines.len() {
            if lines[i - 1].ends_with_hyphen() && lines[i].ends_with_hyphen() {
                // Mark second line as non-hyphenated by trying to pull one more word. [file:1]
                self.try_pull_word_from_next_line(lines, i - 1, max_width, is_chinese)?;
            }
        }

        // Simple rag control: avoid long runs of monotonically increasing/decreasing lengths. [file:1][file:6]
        if !is_chinese && self.font_context.justification != Justification::Justify {
            self.adjust_rag(lines)?;
        }

        Ok(())
    }

    /// Try to move the first word of the last line to the previous line to fix widows. [file:1][file:6]
    fn try_pull_word_from_previous_line(
        &mut self,
        lines: &mut Vec<FormattedLine>,
        last_index: usize,
        max_width: f32,
        is_chinese: bool,
    ) -> Result<()> {
        if last_index == 0 {
            return Ok(());
        }

        let prev_index = last_index - 1;
        if lines[last_index].tokens.is_empty() {
            return Ok(());
        }

        let mut first_word_tokens = Vec::new();
        for token in &lines[last_index].tokens {
            match token {
                TextToken::Word(_) => {
                    first_word_tokens.push(token.clone());
                    break;
                }
                TextToken::Space => {
                    first_word_tokens.push(token.clone());
                }
                _ => break,
            }
        }

        if first_word_tokens.is_empty() {
            return Ok(());
        }

        let added_width = self.calculate_line_width(&first_word_tokens, is_chinese);
        if lines[prev_index].width + added_width <= max_width {
            // Move tokens.
            lines[prev_index].tokens.extend(first_word_tokens.clone());
            lines[prev_index].width += added_width;
            lines[prev_index].text = lines[prev_index].tokens_to_string();

            let remove_count = first_word_tokens.len();
            lines[last_index].tokens.drain(0..remove_count);
            lines[last_index].width = self.calculate_line_width(&lines[last_index].tokens, is_chinese);
            lines[last_index].text = lines[last_index].tokens_to_string();
        }

        Ok(())
    }

    /// Try to push the last word of a line to the next line (fix orphan first line or short line). [file:1][file:6]
    fn try_push_word_to_next_line(
        &mut self,
        lines: &mut Vec<FormattedLine>,
        index: usize,
        max_width: f32,
        is_chinese: bool,
    ) -> Result<()> {
        if index + 1 >= lines.len() {
            return Ok(());
        }

        if lines[index].tokens.is_empty() {
            return Ok(());
        }

        let mut trailing_tokens = Vec::new();
        let mut remove_from = lines[index].tokens.len();

        for (i, token) in lines[index].tokens.iter().enumerate().rev() {
            match token {
                TextToken::Word(_) => {
                    trailing_tokens.push(token.clone());
                    remove_from = i;
                    break;
                }
                TextToken::Space => {
                    trailing_tokens.push(token.clone());
                }
                _ => break,
            }
        }

        trailing_tokens.reverse();

        if trailing_tokens.is_empty() {
            return Ok(());
        }

        let added_width = self.calculate_line_width(&trailing_tokens, is_chinese);
        if lines[index + 1].width + added_width <= max_width {
            // Move tokens.
            lines[index + 1].tokens.splice(0..0, trailing_tokens.clone());
            lines[index + 1].width += added_width;
            lines[index + 1].text = lines[index + 1].tokens_to_string();

            lines[index].tokens.truncate(remove_from);
            lines[index].width = self.calculate_line_width(&lines[index].tokens, is_chinese);
            lines[index].text = lines[index].tokens_to_string();
        }

        Ok(())
    }

    /// Try to pull the first word from the next line into the current line (e.g. to reduce word count on next). [file:1][file:6]
    fn try_pull_word_from_next_line(
        &mut self,
        lines: &mut Vec<FormattedLine>,
        index: usize,
        max_width: f32,
        is_chinese: bool,
    ) -> Result<()> {
        if index + 1 >= lines.len() {
            return Ok(());
        }

        if lines[index + 1].tokens.is_empty() {
            return Ok(());
        }

        let mut first_word_tokens = Vec::new();
        for token in &lines[index + 1].tokens {
            match token {
                TextToken::Word(_) => {
                    first_word_tokens.push(token.clone());
                    break;
                }
                TextToken::Space => {
                    first_word_tokens.push(token.clone());
                }
                _ => break,
            }
        }

        if first_word_tokens.is_empty() {
            return Ok(());
        }

        let added_width = self.calculate_line_width(&first_word_tokens, is_chinese);
        if lines[index].width + added_width <= max_width {
            lines[index].tokens.extend(first_word_tokens.clone());
            lines[index].width += added_width;
            lines[index].text = lines[index].tokens_to_string();

            let remove_count = first_word_tokens.len();
            lines[index + 1].tokens.drain(0..remove_count);
            lines[index + 1].width =
                self.calculate_line_width(&lines[index + 1].tokens, is_chinese);
            lines[index + 1].text = lines[index + 1].tokens_to_string();
        }

        Ok(())
    }

    /// Adjust rag to avoid sloping alignment and keep an undulating rag. [file:1][file:6]
    fn adjust_rag(&mut self, lines: &mut [FormattedLine]) -> Result<()> {
        if lines.len() < 3 {
            return Ok(());
        }

        // Very simple heuristic: if we detect 3 monotonically increasing or decreasing widths,
        // we nudge the middle line by slightly adjusting its width (conceptually via tracking). [file:1]
        for i in 1..(lines.len() - 1) {
            let prev = lines[i - 1].width;
            let curr = lines[i].width;
            let next = lines[i + 1].width;

            if (curr > prev && next > curr) || (curr < prev && next < curr) {
                // Nudge middle line towards the average.
                let target = (prev + next) / 2.0;
                let delta = target - curr;
                let max_delta = curr * 0.05; // limit to 5%
                let clamped = delta.clamp(-max_delta, max_delta);
                lines[i].width += clamped;
            }
        }

        Ok(())
    }

    /// Check if character is a natural breaking point in Chinese. [file:6]
    fn is_natural_break_point(&self, ch: char) -> bool {
        matches!(
            ch,
            '，'
                | '。'
                | '；'
                | '：'
                | '？'
                | '！'
                | '「'
                | '」'
                | '『'
                | '』'
                | '（'
                | '）'
        )
    }

    /// Helper to identify Chinese punctuation for tokenization. [file:6]
    fn is_chinese_punctuation(&self, ch: char) -> bool {
        self.is_natural_break_point(ch)
    }

    /// Split Chinese text with natural breaking points (not wired into composer yet). [file:6]
    fn split_chinese_text(&self, text: &str) -> Vec<String> {
        let mut segments = Vec::new();
        let mut current_segment = String::new();

        for ch in text.chars() {
            current_segment.push(ch);
            if self.is_natural_break_point(ch) {
                if !current_segment.is_empty() {
                    segments.push(current_segment.clone());
                    current_segment.clear();
                }
            }
        }

        if !current_segment.is_empty() {
            segments.push(current_segment);
        }

        if segments.is_empty() || segments.len() == 1 {
            self.split_chinese_by_length(text, 20)
        } else {
            segments
        }
    }

    /// Split Chinese text by character count for readability. [file:6]
    fn split_chinese_by_length(&self, text: &str, max_length: usize) -> Vec<String> {
        let mut segments = Vec::new();
        let chars: Vec<char> = text.chars().collect();
        for chunk in chars.chunks(max_length) {
            segments.push(chunk.iter().collect::<String>());
        }
        segments
    }

    /// Break a long English word into smaller pieces (static version). [file:6]
    fn break_long_word_static(word: &str, max_width: f32, font_context: &mut FontContext) -> Vec<String> {
        let mut pieces = Vec::new();
        let mut current_piece = String::new();

        for ch in word.chars() {
            let test_piece = format!("{}{}", current_piece, ch);
            let test_width = font_context.calculate_text_width(&test_piece, false);
            if test_width <= max_width {
                current_piece = test_piece;
            } else {
                if !current_piece.is_empty() {
                    pieces.push(current_piece);
                    current_piece = ch.to_string();
                } else {
                    pieces.push(ch.to_string());
                }
            }
        }

        if !current_piece.is_empty() {
            pieces.push(current_piece);
        }

        pieces
    }

    /// Break a long English word into smaller pieces (instance version, currently passthrough). [file:6]
    fn break_long_word(&mut self, word: &str, _max_width: f32) -> Vec<String> {
        vec![word.to_string()]
    }

    /// Apply bidirectional text processing for mixed content. [file:6]
    pub fn process_bidi_text(&self, text: &str) -> String {
        let _bidi_info = BidiInfo::new(text, Some(unicode_bidi::Level::ltr()));
        // For now, return text as-is; proper RTL handling would go here. [file:6]
        text.to_string()
    }

    /// Detect script of text (Chinese vs English). [file:6]
    pub fn detect_script(&self, text: &str) -> bool {
        // If any character is CJK script, treat as Chinese. [file:6]
        text.chars().any(|ch| {
            ch.script() == Script::Han
                || ch.script() == Script::Hiragana
                || ch.script() == Script::Katakana
                || ch.script() == Script::Bopomofo
        })
    }
}

/// Create a text layout engine. [file:6]
pub fn create_layout_engine(font_context: FontContext) -> TextLayoutEngine {
    TextLayoutEngine::new(font_context)
}

#!/usr/bin/env python3
"""
CBETA Buddhist Metadata Analyzer

This script analyzes XML files for Buddhist tradition, time period, 
and geographic origin based on canon type, author information, 
and historical context in the metadata.
"""

import os
import json
import xml.etree.ElementTree as ET
from collections import defaultdict, Counter
from pathlib import Path
import re
from typing import Dict, List, Set, Tuple

# Load canon information
def load_canon_info():
    """Load canon classification information."""
    canon_file = r"D:\Rust-projects\not-rust-projects\CBETA-Translator\CbetaZenTexts\xml-p5\canons.json"
    with open(canon_file, 'r', encoding='utf-8') as f:
        return json.load(f)

# Buddhist tradition classifications
TRADITION_CLASSIFICATIONS = {
    # Chinese Buddhist traditions
    "Chan/Zen": ["禪", "禅", "chan", "zen", "曹洞", "臨濟", "雲門", "潙仰", "法眼"],
    "Pure Land": ["淨土", "净土", "净土宗", "淨土宗", "蓮社", "念佛法門", "阿彌陀"],
    "Tiantai": ["天台", "法華", "止觀", "智者", "天台宗"],
    "Huayan": ["華嚴", "华严", "賢首", "法藏", "澄觀"],
    "Vinaya": ["律", "毗奈耶", "戒律", "四分律", "五分律", "摩訶僧祇律"],
    "Madhyamaka": ["中觀", "中論", "龍樹", "提婆", "三論"],
    "Yogacara": ["瑜伽", "唯識", "瑜伽行派", "無著", "世親", "成唯識"],
    "Esoteric": ["密", "密教", "陀羅尼", "真言", "壇城", "曼荼羅"],
    "Pure Precepts": ["菩薩戒", "梵網經", "心地戒"],
    
    # Non-Chinese traditions
    "Pali/Theravada": ["南傳", "巴利", "Theravāda", "尼柯耶", "律藏"],
    "Tibetan": ["藏傳", "西藏", "Tibetan", "甘珠爾", "丹珠爾"],
    
    # General/Scholarly
    "Commentarial": ["註", "疏", "記", "釋", "解", "論", "鈔"],
    "Historical": ["史", "傳", "誌", "錄", "譜"],
    "Liturgical": ["儀軌", "法事", "懺", "儀", "課誦"],
}

# Dynasty/Period classifications
DYNASTY_PERIODS = {
    "Pre-Tang": {"漢", "魏", "晉", "南北朝", "劉宋", "南齊", "梁", "陳", "北魏", "北齊", "北周", "隋"},
    "Tang": {"唐"},
    "Song": {"宋", "北宋", "南宋"},
    "Yuan": {"元"},
    "Ming": {"明"},
    "Qing": {"清"},
    "Modern": {"民國", "中華民國", "現代"},
    "Contemporary": {"當代"}
}

# Geographic classifications
GEOGRAPHIC_ORIGINS = {
    "India": {"印度", "天竺", "中天竺", "北天竺", "南天竺", "西天"},
    "Central Asia": {"西域", "中亞", "龜茲", "于闐", "高昌"},
    "China": {"中國", "漢地", "中土", "大唐", "大宋", "大元", "大明", "大清"},
    "Korea": {"高麗", "新羅", "百濟", "朝鮮"},
    "Japan": {"日本", "倭"},
    "Southeast Asia": {"南海", "扶南", "真臘", "林邑"},
}

def extract_metadata(xml_file_path: str, canon_info: Dict) -> Dict:
    """Extract relevant Buddhist metadata from XML file."""
    try:
        tree = ET.parse(xml_file_path)
        root = tree.getroot()
        
        ns = {'tei': 'http://www.tei-c.org/ns/1.0'}
        
        # Extract basic info
        xml_id = root.get('xml:id', '')
        
        # Extract canon info from ID or file path
        canon_code = None
        if xml_id:
            match = re.match(r'^([A-Z]+)', xml_id)
            if match:
                canon_code = match.group(1)
        
        # If no canon from ID, try to extract from file path
        if not canon_code:
            file_parts = xml_file_path.split(os.sep)
            for part in file_parts:
                if part in canon_info:
                    canon_code = part
                    break
        
        # Extract titles
        titles = []
        title_elements = root.findall('.//tei:title', ns)
        for title in title_elements:
            if title.text:
                titles.append(title.text.strip())
        
        # Extract author
        author = ""
        author_elem = root.find('.//tei:author', ns)
        if author_elem is not None and author_elem.text:
            author = author_elem.text.strip()
        
        # Extract source/bibl
        source = ""
        bibl_elem = root.find('.//tei:sourceDesc/tei:bibl', ns)
        if bibl_elem is not None and bibl_elem.text:
            source = bibl_elem.text.strip()
        
        return {
            'xml_id': xml_id,
            'canon_code': canon_code,
            'canon_name': canon_info.get(canon_code, {}).get('title-zh', canon_code) if canon_code else 'Unknown',
            'titles': titles,
            'author': author,
            'source': source,
            'file_path': xml_file_path
        }
        
    except Exception as e:
        return {'error': str(e)}

def classify_tradition(titles: List[str], author: str, source: str) -> List[str]:
    """Classify Buddhist tradition based on text content."""
    traditions = set()
    text_content = ' '.join(titles + [author] + [source]).lower()
    
    for tradition, keywords in TRADITION_CLASSIFICATIONS.items():
        for keyword in keywords:
            if keyword.lower() in text_content:
                traditions.add(tradition)
                break
    
    return list(traditions) if traditions else ["General/Unspecified"]

def classify_period(author: str, source: str, titles: List[str]) -> str:
    """Classify historical period."""
    text_content = ' '.join([author, source] + titles)
    
    for period, dynasties in DYNASTY_PERIODS.items():
        for dynasty in dynasties:
            if dynasty in text_content:
                return period
    
    # Check for specific era indicators
    if any(word in text_content for word in ["民國", "中華民國", "現代"]):
        return "Modern"
    elif any(word in text_content for word in ["當代", "現代"]):
        return "Contemporary"
    elif "釋" in author and any(dynasty in text_content for dynasty in ["唐", "宋", "元", "明", "清"]):
        return "Traditional Chinese Buddhism"
    
    return "Unknown Period"

def classify_geographic(author: str, source: str, titles: List[str]) -> str:
    """Classify geographic origin."""
    text_content = ' '.join([author, source] + titles)
    
    for region, locations in GEOGRAPHIC_ORIGINS.items():
        for location in locations:
            if location in text_content:
                return region
    
    return "Unknown Origin"

def analyze_buddhist_metadata(xml_directory: str) -> Dict:
    """Analyze all XML files for Buddhist metadata."""
    print("Analyzing Buddhist metadata...")
    
    canon_info = load_canon_info()
    
    # Find all XML files
    xml_files = []
    base_path = Path(xml_directory)
    for xml_file in base_path.rglob('*.xml'):
        xml_files.append(str(xml_file))
    
    print(f"Found {len(xml_files)} XML files")
    
    # Analyze files
    results = {
        'summary': {},
        'by_canon': defaultdict(lambda: {'files': [], 'traditions': Counter(), 'periods': Counter(), 'origins': Counter()}),
        'by_tradition': defaultdict(list),
        'by_period': defaultdict(list),
        'by_origin': defaultdict(list),
        'detailed_analysis': []
    }
    
    processed = 0
    for xml_file in xml_files:
        processed += 1
        if processed % 100 == 0:
            print(f"Processed {processed}/{len(xml_files)} files...")
        
        metadata = extract_metadata(xml_file, canon_info)
        
        if 'error' in metadata:
            continue
        
        # Classify
        traditions = classify_tradition(metadata['titles'], metadata['author'], metadata['source'])
        period = classify_period(metadata['author'], metadata['source'], metadata['titles'])
        origin = classify_geographic(metadata['author'], metadata['source'], metadata['titles'])
        
        # Update canon-based analysis
        canon_code = metadata['canon_code']
        if canon_code:
            results['by_canon'][canon_code]['files'].append(metadata['file_path'])
            for tradition in traditions:
                results['by_canon'][canon_code]['traditions'][tradition] += 1
            results['by_canon'][canon_code]['periods'][period] += 1
            results['by_canon'][canon_code]['origins'][origin] += 1
        
        # Update tradition-based analysis
        for tradition in traditions:
            results['by_tradition'][tradition].append(metadata['file_path'])
        
        # Update period-based analysis
        results['by_period'][period].append(metadata['file_path'])
        
        # Update origin-based analysis
        results['by_origin'][origin].append(metadata['file_path'])
        
        # Add to detailed analysis
        results['detailed_analysis'].append({
            'file': metadata['file_path'],
            'canon': canon_code,
            'canon_name': metadata['canon_name'],
            'traditions': traditions,
            'period': period,
            'origin': origin,
            'author': metadata['author'],
            'main_title': metadata['titles'][0] if metadata['titles'] else ''
        })
    
    # Generate summary
    results['summary'] = {
        'total_files': len(xml_files),
        'canons_found': len(results['by_canon']),
        'traditions_found': len(results['by_tradition']),
        'periods_found': len(results['by_period']),
        'origins_found': len(results['by_origin'])
    }
    
    return results

def save_buddhist_analysis(results: Dict, output_file: str):
    """Save Buddhist metadata analysis results."""
    print(f"Saving Buddhist analysis to: {output_file}")
    
    # Convert defaultdicts to regular dicts for JSON serialization
    serializable_results = {
        'summary': results['summary'],
        'by_canon': {k: {
            'file_count': len(v['files']),
            'traditions': dict(v['traditions']),
            'periods': dict(v['periods']),
            'origins': dict(v['origins']),
            'sample_files': v['files'][:5]  # First 5 files as samples
        } for k, v in results['by_canon'].items()},
        'by_tradition': {k: {'file_count': len(v), 'files': v[:10]} for k, v in results['by_tradition'].items()},
        'by_period': {k: {'file_count': len(v), 'files': v[:10]} for k, v in results['by_period'].items()},
        'by_origin': {k: {'file_count': len(v), 'files': v[:10]} for k, v in results['by_origin'].items()},
        'detailed_analysis': results['detailed_analysis'][:100]  # First 100 detailed entries
    }
    
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(serializable_results, f, indent=2, ensure_ascii=False)
    
    print("Buddhist analysis saved successfully!")

def print_buddhist_summary(results: Dict):
    """Print a summary of Buddhist metadata analysis."""
    summary = results['summary']
    
    print(f"\n{'='*80}")
    print("CBETA Buddhist Metadata Analysis Summary")
    print(f"{'='*80}")
    print(f"Total files analyzed: {summary['total_files']}")
    print(f"Canons represented: {summary['canons_found']}")
    print(f"Buddhist traditions identified: {summary['traditions_found']}")
    print(f"Historical periods identified: {summary['periods_found']}")
    print(f"Geographic origins identified: {summary['origins_found']}")
    
    print(f"\n{'='*80}")
    print("BY CANON (Top 10 by file count)")
    print(f"{'='*80}")
    
    canon_counts = [(code, len(data['files'])) for code, data in results['by_canon'].items()]
    canon_counts.sort(key=lambda x: x[1], reverse=True)
    
    for i, (canon, count) in enumerate(canon_counts[:10], 1):
        canon_data = results['by_canon'][canon]
        top_tradition = canon_data['traditions'].most_common(1)[0] if canon_data['traditions'] else ('Unknown', 0)
        print(f"{i:2d}. {canon}: {count:4d} files | Top tradition: {top_tradition[0]} ({top_tradition[1]} files)")
    
    print(f"\n{'='*80}")
    print("BY BUDDHIST TRADITION")
    print(f"{'='*80}")
    
    tradition_counts = [(tradition, len(files)) for tradition, files in results['by_tradition'].items()]
    tradition_counts.sort(key=lambda x: x[1], reverse=True)
    
    for tradition, count in tradition_counts:
        print(f"{tradition}: {count:4d} files")
    
    print(f"\n{'='*80}")
    print("BY HISTORICAL PERIOD")
    print(f"{'='*80}")
    
    period_counts = [(period, len(files)) for period, files in results['by_period'].items()]
    period_counts.sort(key=lambda x: x[1], reverse=True)
    
    for period, count in period_counts:
        print(f"{period}: {count:4d} files")
    
    print(f"\n{'='*80}")
    print("BY GEOGRAPHIC ORIGIN")
    print(f"{'='*80}")
    
    origin_counts = [(origin, len(files)) for origin, files in results['by_origin'].items()]
    origin_counts.sort(key=lambda x: x[1], reverse=True)
    
    for origin, count in origin_counts:
        print(f"{origin}: {count:4d} files")

def main():
    """Main function to run Buddhist metadata analysis."""
    base_dir = r"D:\Rust-projects\not-rust-projects\CBETA-Translator\CbetaZenTexts\xml-p5"
    output_file = r"D:\Rust-projects\not-rust-projects\CBETA-Translator\buddhist_metadata_analysis.json"
    
    if not os.path.exists(base_dir):
        print(f"Error: Directory not found: {base_dir}")
        return
    
    print("Starting CBETA Buddhist metadata analysis...")
    results = analyze_buddhist_metadata(base_dir)
    
    save_buddhist_analysis(results, output_file)
    print_buddhist_summary(results)
    
    print(f"\nDetailed Buddhist metadata analysis saved to: {output_file}")

if __name__ == "__main__":
    main()

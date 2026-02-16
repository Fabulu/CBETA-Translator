#!/usr/bin/env python3
"""
Generate sorting recommendations for CBETA texts based on Buddhist metadata.
"""

import json
from collections import defaultdict

def generate_sorting_recommendations():
    """Generate comprehensive sorting recommendations."""
    
    # Load the Buddhist metadata analysis
    with open('buddhist_metadata_analysis.json', 'r', encoding='utf-8') as f:
        buddhist_data = json.load(f)
    
    # Load the projectDesc analysis
    with open('projectdesc_analysis.json', 'r', encoding='utf-8') as f:
        project_data = json.load(f)
    
    recommendations = []
    
    print("=" * 80)
    print("CBETA TEXT LIBRARY SORTING RECOMMENDATIONS")
    print("=" * 80)
    
    print("\n📊 OVERVIEW:")
    print(f"• Total files: {buddhist_data['summary']['total_files']}")
    print(f"• Available metadata: Canon types, Buddhist traditions, Historical periods, Geographic origins, Project contributors")
    
    print("\n" + "=" * 80)
    print("🏛️ 1. SORTING BY BUDDHIST TRADITION/SCHOOL (RECOMMENDED)")
    print("=" * 80)
    
    traditions = buddhist_data['by_tradition']
    print("This approach groups texts by their philosophical school or practice tradition:")
    print("\nMajor traditions with significant content:")
    
    sorted_traditions = sorted(traditions.items(), key=lambda x: x[1]['file_count'], reverse=True)
    
    for tradition, data in sorted_traditions[:10]:
        count = data['file_count']
        if count > 50:  # Only show meaningful groups
            print(f"  • {tradition}: {count:4d} files")
    
    print(f"\n✅ PROS: Theologically meaningful, good for scholarly study")
    print("❌ CONS: Many texts fall into 'General/Unspecified' category")
    print("📁 FOLDER STRUCTURE: /Tradition/Chan_Zen/, /Tradition/Pure_Land/, etc.")
    
    print("\n" + "=" * 80)
    print("📚 2. SORTING BY CANON TYPE (EXCELLENT FOR ORGANIZATION)")
    print("=" * 80)
    
    canons = buddhist_data['by_canon']
    print("This approach groups texts by their source canon/collection:")
    
    sorted_canons = sorted(canons.items(), key=lambda x: x[1]['file_count'], reverse=True)
    
    # Load canon info for names
    with open(r"D:\Rust-projects\not-rust-projects\CBETA-Translator\CbetaZenTexts\xml-p5\canons.json", 'r', encoding='utf-8') as f:
        canon_info = json.load(f)
    
    for canon, data in sorted_canons[:10]:
        count = data['file_count']
        canon_name = canon_info.get(canon, {}).get('title-zh', canon)
        print(f"  • {canon} ({canon_name}): {count:4d} files")
    
    print(f"\n✅ PROS: Clear, well-defined categories, reflects historical development")
    print("❌ CONS: Mixes different traditions within each canon")
    print("📁 FOLDER STRUCTURE: /Canon/T/, /Canon/X/, /Canon/J/, etc.")
    
    print("\n" + "=" * 80)
    print("⏰ 3. SORTING BY HISTORICAL PERIOD")
    print("=" * 80)
    
    periods = buddhist_data['by_period']
    print("This approach groups texts by the dynasty/time period they were composed:")
    
    sorted_periods = sorted(periods.items(), key=lambda x: x[1]['file_count'], reverse=True)
    
    for period, data in sorted_periods:
        count = len(data['files'])
        if count > 50:
            print(f"  • {period}: {count:4d} files")
    
    print(f"\n✅ PROS: Historically meaningful, good for diachronic studies")
    print("❌ CONS: Many texts have unknown periods, spans multiple dynasties")
    print("📁 FOLDER STRUCTURE: /Period/Tang/, /Period/Song/, /Period/Ming/, etc.")
    
    print("\n" + "=" * 80)
    print("🌍 4. SORTING BY GEOGRAPHIC ORIGIN")
    print("=" * 80)
    
    origins = buddhist_data['by_origin']
    print("This approach groups texts by their geographic origin:")
    
    sorted_origins = sorted(origins.items(), key=lambda x: x[1]['file_count'], reverse=True)
    
    for origin, data in sorted_origins:
        count = len(data['files'])
        if count > 10:
            print(f"  • {origin}: {count:4d} files")
    
    print(f"\n✅ PROS: Shows cultural transmission patterns")
    print("❌ CONS: Many texts have unknown origins, most are Chinese adaptations")
    print("📁 FOLDER STRUCTURE: /Origin/China/, /Origin/Japan/, /Origin/India/, etc.")
    
    print("\n" + "=" * 80)
    print("👥 5. SORTING BY PROJECT CONTRIBUTORS (PROCESSING-FOCUSED)")
    print("=" * 80)
    
    print("This approach groups texts by who digitized/processed them:")
    
    project_groups = project_data['project_groups']
    sorted_projects = sorted(project_groups.items(), key=lambda x: x[1]['file_count'], reverse=True)
    
    print("Top contributor groups:")
    for i, (project_desc, data) in enumerate(sorted_projects[:8], 1):
        count = data['file_count']
        # Truncate for display
        display_desc = project_desc[:60] + "..." if len(project_desc) > 60 else project_desc
        print(f"  {i}. {count:4d} files: {display_desc}")
    
    print(f"\n✅ PROS: Reflects digitization history, good for quality assessment")
    print("❌ CONS: No scholarly meaning, 486 different categories")
    print("📁 FOLDER STRUCTURE: /Contributor/CBETA/, /Contributor/Christian_Wittern/, etc.")
    
    print("\n" + "=" * 80)
    print("🎯 RECOMMENDED HYBRID APPROACH")
    print("=" * 80)
    
    print("Based on the analysis, I recommend a multi-level sorting system:")
    print()
    print("📁 PRIMARY LEVEL: Canon Type (26 major categories)")
    print("  • Clear, well-defined boundaries")
    print("  • Reflects historical collections")
    print("  • Manageable number of main folders")
    print()
    print("📁 SECONDARY LEVEL: Buddhist Tradition (within each canon)")
    print("  • Theologically meaningful subcategories")
    print("  • Good for scholarly research")
    print("  • Handle 'General/Unspecified' as catch-all")
    print()
    print("📁 TERTIARY LEVEL: Historical Period (optional)")
    print("  • For large canons like T (Taishō) and X (Xuzang)")
    print("  • Separate 'Unknown Period' folder")
    
    print("\n" + "=" * 80)
    print("🗂️  EXAMPLE FOLDER STRUCTURE")
    print("=" * 80)
    
    print("""
CBETA_Texts/
├── Canon_T/                    # Taishō Tripiṭaka (2,471 files)
│   ├── Tradition_Chan_Zen/      # Zen texts
│   ├── Tradition_Pure_Land/     # Pure Land texts
│   ├── Tradition_Vinaya/        # Monastic discipline
│   ├── Tradition_Commentarial/  # Commentaries
│   ├── Period_Tang/            # Tang dynasty texts
│   ├── Period_Song/            # Song dynasty texts
│   └── General_Unspecified/    # Other texts
├── Canon_X/                    # Xuzang (1,236 files)
│   ├── Tradition_Commentarial/  # Mostly commentaries
│   ├── Tradition_Chan_Zen/      # Zen texts
│   └── General_Unspecified/
├── Canon_J/                    # Jiaxing Canon (287 files)
│   ├── Tradition_Historical/    # Historical documents
│   └── General_Unspecified/
└── [Other 23 canons...]
    """)
    
    print("\n" + "=" * 80)
    print("📈 SORTING PRIORITY RECOMMENDATIONS")
    print("=" * 80)
    
    print("1. 🥇 Canon-based sorting (highest priority)")
    print("   • Clear boundaries, historically meaningful")
    print("   • 26 manageable main categories")
    print()
    print("2. 🥈 Tradition-based sorting (secondary priority)")
    print("   • Theologically significant")
    print("   • Good for scholarly research")
    print("   • 15 major traditions identified")
    print()
    print("3. 🥉 Period-based sorting (optional)")
    print("   • Use only for large collections")
    print("   • Many texts have unknown periods")
    print()
    print("4. ❌ Contributor-based sorting (not recommended for final organization)")
    print("   • Too many categories (486)")
    print("   • No scholarly meaning")
    print("   • Useful for tracking digitization quality only")
    
    print("\n" + "=" * 80)
    print("✅ FINAL RECOMMENDATION")
    print("=" * 80)
    
    print("Use **Canon → Tradition → (optional) Period** sorting hierarchy.")
    print("This provides:")
    print("• 🏛️ Historical context (canon)")
    print("• 🧘 Theological meaning (tradition)") 
    print("• ⏰ Chronological organization (period)")
    print("• 📁 Manageable folder structure")
    print("• 🔍 Scholarly utility")

if __name__ == "__main__":
    generate_sorting_recommendations()

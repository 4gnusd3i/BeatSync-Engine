"""Preset values shared between clients and services."""

from __future__ import annotations

SMART_PRESETS_CONFIG = {
    "slower": {
        "cut_frequency": "every_4th_strong_beat",
        "kick_threshold": 70,
        "clap_threshold": 70,
        "min_interval": 1.5,
        "description": "Cinematic - Every 4th strong beat (fewest cuts)",
    },
    "slow": {
        "cut_frequency": "every_2nd_strong_beat",
        "kick_threshold": 60,
        "clap_threshold": 60,
        "min_interval": 0.8,
        "description": "Relaxed - Every 2nd strong beat",
    },
    "normal": {
        "cut_frequency": "every_strong_beat",
        "kick_threshold": 50,
        "clap_threshold": 50,
        "min_interval": 0.4,
        "description": "Standard - Every strong kick or clap",
    },
    "fast": {
        "cut_frequency": "all_beats_prioritize_strong",
        "kick_threshold": 40,
        "clap_threshold": 40,
        "min_interval": 0.25,
        "description": "Energetic - All beats, prioritize strong",
    },
    "faster": {
        "cut_frequency": "all_beats_plus_subdivisions",
        "kick_threshold": 30,
        "clap_threshold": 30,
        "min_interval": 0.15,
        "description": "Hyper - All beats + subdivisions (most cuts)",
    },
}

RESOLUTION_PRESET_CHOICES = [
    ("Default (match first source video)", "default"),
    ("16:9 | 1280x720 (HD)", "1280x720"),
    ("16:9 | 1920x1080 (Full HD)", "1920x1080"),
    ("16:9 | 2560x1440 (QHD)", "2560x1440"),
    ("16:9 | 3840x2160 (4K UHD)", "3840x2160"),
    ("21:9 | 2560x1080 (UltraWide HD)", "2560x1080"),
    ("21:9 | 3440x1440 (UltraWide QHD)", "3440x1440"),
    ("21:9 | 3840x1600 (UltraWide 1600p)", "3840x1600"),
    ("9:16 | 720x1280 (Vertical HD)", "720x1280"),
    ("9:16 | 1080x1920 (Vertical Full HD)", "1080x1920"),
    ("9:16 | 1440x2560 (Vertical QHD)", "1440x2560"),
]

STANDARD_QUALITY_LABELS = [("Fast", "fast"), ("Balanced", "balanced"), ("High", "high")]


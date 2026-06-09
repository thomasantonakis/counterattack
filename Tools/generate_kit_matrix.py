#!/usr/bin/env python3
"""Generate the kit similarity Markdown matrix.

Run from the repository root:
    python3 Tools/generate_kit_matrix.py
"""

from __future__ import annotations

import json
import math
from dataclasses import dataclass
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
SOURCE_JSON = ROOT / "Tools" / "kit-picker-v1-11-supported.json"
OUTPUT_MD = ROOT / "Tools" / "kit-similarity-matrix.md"

FACE_AVERAGE_SAMPLE_TEXTURE_SIZE = 64
FACE_DOMINANT_SAMPLE_TEXTURE_SIZE = 64
FACE_PATTERN_SAMPLE_TEXTURE_SIZE = 32
FACE_INNER_RADIUS_FOR_SIMILARITY = 0.85
FACE_CIRCLE_RADIUS = 0.90
FACE_RING_THICKNESS = 0.05
FACE_INNER_RADIUS = FACE_CIRCLE_RADIUS - FACE_RING_THICKNESS
VERTICAL_CENTER_STRIPE_GAP_BOTTOM = -0.60
VERTICAL_CENTER_STRIPE_GAP_TOP = 0.48
SLEEVE_MIN_V = -0.06
SLEEVE_DIAGONAL_BASE_X = 0.60
SLEEVE_DIAGONAL_SLOPE = 0.45
ROMA_TOP_STRIPE_MIN_V = 0.50
ROMA_TOP_STRIPE_MAX_V = 0.64
ROMA_BOTTOM_STRIPE_MIN_V = 0.38
ROMA_BOTTOM_STRIPE_MAX_V = 0.50
SAME_COLOR_TOLERANCE = 0.015
BODY_WEIGHT = 2.0 / 3.0
TOP_WEIGHT = 1.0 / 3.0
COMPONENT_THRESHOLD = 77.0
TOTAL_THRESHOLD = 77.0
SHARED_WHITE_TOP_FACE_WEIGHT = 0.20
WHITEISH_LUMINANCE_THRESHOLD = 0.78
WHITEISH_CHROMA_THRESHOLD = 0.16
GREEN = "🟢"
RED = "🔴"


Color = tuple[float, float, float, float]


@dataclass(frozen=True)
class Style:
    family: str
    body_color: Color
    accent_color: Color
    ring_color: Color
    left_stripe_color: Color
    left_mid_stripe_color: Color
    center_stripe_color: Color
    right_mid_stripe_color: Color
    right_stripe_color: Color
    stripe_width: float
    stripe_direction_degrees: float
    number_color: Color
    center_stripe_mode: str


@dataclass(frozen=True)
class Kit:
    kit_id: str
    display_name: str
    style: Style


def main() -> None:
    kits = load_kits()
    lines = [
        "# Kit Similarity Matrix",
        "",
        f"Generated from `{SOURCE_JSON.relative_to(ROOT)}` by `{Path(__file__).relative_to(ROOT)}`.",
        "",
        "Each cell shows body, top-face, and weighted total similarity on separate lines.",
        "`B` and `F` are green at `<= .770`; `T` is green only when `< .770`, matching the Create/Start gate.",
        "",
        build_table(kits),
        "",
    ]
    OUTPUT_MD.write_text("\n".join(lines), encoding="utf-8")
    print(f"Wrote {OUTPUT_MD.relative_to(ROOT)} with {len(kits)} kits.")


def load_kits() -> list[Kit]:
    data = json.loads(SOURCE_JSON.read_text(encoding="utf-8"))
    kits: list[Kit] = []
    for kit_id, raw in data.items():
        style = parse_style(raw)
        if style is None:
            continue
        kits.append(Kit(kit_id=kit_id, display_name=primary_label(raw) or kit_id, style=style))
    return kits


def parse_style(raw: dict[str, Any]) -> Style | None:
    pattern = raw.get("pattern") or ""
    config = raw.get("config") or {}

    if pattern == "Plain":
        body = parse_color(config.get("bodyColor"), WHITE)
        accent = parse_color(config.get("accentColor"), body)
        ring = parse_color(config.get("ringColor"), body)
        return style("Plain", body, accent, ring, number_color=parse_color(config.get("numberColor"), WHITE))

    if pattern == "VerticalStripes":
        body = parse_color(config.get("bodyColor"), WHITE)
        return style(
            "VerticalStripes",
            body,
            CLEAR,
            parse_color(config.get("ringColor"), body),
            left=parse_color(config.get("leftStripeColor"), body),
            left_mid=parse_color(config.get("leftMidStripeColor"), body),
            center=parse_color(config.get("centerStripeColor"), body),
            right_mid=parse_color(config.get("rightMidStripeColor"), body),
            right=parse_color(config.get("rightStripeColor"), body),
            number_color=parse_color(config.get("numberColor"), WHITE),
            mode=parse_center_mode(config.get("centerStripeMode")),
        )

    if pattern == "HorizontalStripes":
        body = parse_color(config.get("bodyColor"), WHITE)
        return style(
            "HorizontalStripes",
            body,
            CLEAR,
            parse_color(config.get("ringColor"), body),
            left=parse_color(config.get("topStripeColor"), parse_color(config.get("leftStripeColor"), body)),
            left_mid=parse_color(config.get("topMidStripeColor"), parse_color(config.get("leftMidStripeColor"), body)),
            center=parse_color(config.get("centerStripeColor"), body),
            right_mid=parse_color(config.get("bottomMidStripeColor"), parse_color(config.get("rightMidStripeColor"), body)),
            right=parse_color(config.get("bottomStripeColor"), parse_color(config.get("rightStripeColor"), body)),
            number_color=parse_color(config.get("numberColor"), WHITE),
            mode=parse_center_mode(config.get("centerStripeMode")),
        )

    if pattern == "VerticalThreeColors":
        body = parse_color(config.get("bodyColor"), WHITE)
        accent = parse_color(config.get("accentColor"), body)
        center = parse_color(config.get("centerStripeColor"), accent)
        return style(
            "VerticalStripes",
            body,
            CLEAR,
            body,
            left=body,
            left_mid=accent,
            center=center,
            right_mid=accent,
            right=body,
            number_color=parse_color(config.get("numberColor"), WHITE),
            mode=parse_center_mode(config.get("centerStripeMode")),
        )

    if pattern == "HorizontalThreeColors":
        body = parse_color(config.get("bodyColor"), WHITE)
        accent = parse_color(config.get("accentColor"), body)
        center = parse_color(config.get("centerStripeColor"), accent)
        return style(
            "HorizontalStripes",
            body,
            CLEAR,
            body,
            left=body,
            left_mid=accent,
            center=center,
            right_mid=accent,
            right=body,
            number_color=parse_color(config.get("numberColor"), WHITE),
            mode=parse_center_mode(config.get("centerStripeMode")),
        )

    if pattern in {"singleStriped", "SingleStriped"} or (pattern == "Sleeved" and looks_like_single_striped(config)):
        body = parse_color(config.get("bodyColor"), WHITE)
        stripe_color = parse_color(config.get("stripeColor"), body)
        return style(
            "SingleStriped",
            body,
            stripe_color,
            stripe_color,
            stripe_width=parse_stripe_width(config.get("stripeWidth")),
            stripe_direction_degrees=parse_stripe_direction(config.get("stripeDirection"), config.get("stripeRotation")),
            number_color=parse_color(config.get("numberColor"), WHITE),
        )

    if pattern == "Sleeved":
        body = parse_color(config.get("bodyColor"), WHITE)
        sleeve = parse_color(config.get("sleeveColor"), body)
        return style("Sleeved", body, body, sleeve, number_color=parse_color(config.get("numberColor"), sleeve))

    if pattern == "Roma":
        body = parse_color(config.get("bodyColor"), WHITE)
        return style(
            "Roma",
            body,
            parse_color(config.get("topStripeColor"), body),
            parse_color(config.get("bottomStripeColor"), body),
            number_color=parse_color(config.get("numberColor"), WHITE),
        )

    return None


def style(
    family: str,
    body: Color,
    accent: Color,
    ring: Color,
    *,
    left: Color = (0.0, 0.0, 0.0, 0.0),
    left_mid: Color = (0.0, 0.0, 0.0, 0.0),
    center: Color = (0.0, 0.0, 0.0, 0.0),
    right_mid: Color = (0.0, 0.0, 0.0, 0.0),
    right: Color = (0.0, 0.0, 0.0, 0.0),
    stripe_width: float = 0.0,
    stripe_direction_degrees: float = 0.0,
    number_color: Color = (1.0, 1.0, 1.0, 1.0),
    mode: str = "None",
) -> Style:
    return Style(family, body, accent, ring, left, left_mid, center, right_mid, right, stripe_width, stripe_direction_degrees, number_color, mode)


def build_table(kits: list[Kit]) -> str:
    headers = ["Kit"] + [escape_cell(short_label(kit)) for kit in kits]
    separator = ["---"] * len(headers)
    rows = ["| " + " | ".join(headers) + " |", "| " + " | ".join(separator) + " |"]
    for row_kit in kits:
        cells = [escape_cell(short_label(row_kit))]
        for column_kit in kits:
            cells.append(format_scores(row_kit.style, column_kit.style))
        rows.append("| " + " | ".join(cells) + " |")
    return "\n".join(rows)


def format_scores(a: Style, b: Style) -> str:
    body, top, total = similarity(a, b)
    body_icon = GREEN if body <= COMPONENT_THRESHOLD else RED
    top_icon = GREEN if top <= COMPONENT_THRESHOLD else RED
    total_icon = GREEN if total < TOTAL_THRESHOLD else RED
    return f"B: {ratio(body)}{body_icon}<br>F: {ratio(top)}{top_icon}<br>T: {ratio(total)}{total_icon}"


def similarity(a: Style, b: Style) -> tuple[float, float, float]:
    body = color_similarity(a.body_color, b.body_color)
    face_mean = color_similarity(average_face_color(a, FACE_AVERAGE_SAMPLE_TEXTURE_SIZE), average_face_color(b, FACE_AVERAGE_SAMPLE_TEXTURE_SIZE))
    top = top_face_similarity(a, b, face_mean)
    total = (body * BODY_WEIGHT) + (top * TOP_WEIGHT)
    return body, top, total


def top_face_similarity(a: Style, b: Style, face_mean: float) -> float:
    direct = top_face_pixel_similarity(a, b, swap_second_palette=False)
    swapped = top_face_pixel_similarity(a, b, swap_second_palette=True)
    structured = structured_top_face_similarity(a, b)
    rendered = max(direct, swapped)
    rendered_mean = (rendered * 0.90) + (face_mean * 0.10)
    return clamp(max(structured, rendered_mean), 0.0, 100.0)


def top_face_pixel_similarity(a: Style, b: Style, *, swap_second_palette: bool) -> float:
    palette_b = top_face_palette(b) if swap_second_palette else None
    accumulated = 0.0
    count = 0
    size = FACE_PATTERN_SAMPLE_TEXTURE_SIZE
    for i in range(size * size):
        if not is_inner_face_pixel(i, size):
            continue
        x = i % size
        y = i // size
        pixel_a = decal_pixel(a, x, y, size)
        pixel_b = decal_pixel(b, x, y, size)
        if pixel_a[3] <= 0.0 or pixel_b[3] <= 0.0:
            continue
        if palette_b is not None:
            pixel_b = swap_palette_color(pixel_b, palette_b)
        weight = top_face_color_pair_weight(pixel_a, pixel_b)
        accumulated += color_similarity(pixel_a, pixel_b) * weight
        count += weight
    return 0.0 if count == 0.0 else accumulated / count


def structured_top_face_similarity(a: Style, b: Style) -> float:
    if a.family == b.family and a.family in {"VerticalStripes", "HorizontalStripes"}:
        stripe_score = stripe_slot_similarity(a, b)
        mode_score = 100.0 if a.center_stripe_mode == b.center_stripe_mode else 35.0
        return (stripe_score * 0.92) + (mode_score * 0.08)
    if a.family == b.family:
        return surface_pattern_similarity(a, b)
    return 0.0


def stripe_slot_similarity(a: Style, b: Style) -> float:
    slots_a = stripe_slots(a)
    slots_b = stripe_slots(b)
    direct = average_slot_similarity(slots_a, slots_b)
    palette_b = top_face_palette(b)
    swapped = average_slot_similarity(slots_a, [swap_palette_color(slot, palette_b) for slot in slots_b])
    return max(direct, swapped)


def surface_pattern_similarity(a: Style, b: Style) -> float:
    if a.family != b.family:
        return 0.0
    if a.family == "Plain":
        return color_similarity(a.accent_color, b.accent_color)
    if a.family == "Sleeved":
        return (color_similarity(a.body_color, b.body_color) + color_similarity(a.ring_color, b.ring_color)) / 2.0
    if a.family == "SingleStriped":
        color_score = color_similarity(a.accent_color, b.accent_color)
        width_score = 100.0 - (abs(a.stripe_width - b.stripe_width) * 100.0)
        direction_difference = abs(a.stripe_direction_degrees - b.stripe_direction_degrees)
        direction_difference = min(direction_difference, 180.0 - direction_difference)
        direction_score = 100.0 - ((direction_difference / 90.0) * 100.0)
        return (color_score + clamp(width_score, 0.0, 100.0) + clamp(direction_score, 0.0, 100.0)) / 3.0
    if a.family == "Roma":
        return (color_similarity(a.accent_color, b.accent_color) + color_similarity(a.ring_color, b.ring_color)) / 2.0
    return average_slot_similarity(stripe_slots(a), stripe_slots(b))


def average_slot_similarity(slots_a: list[Color], slots_b: list[Color]) -> float:
    accumulated = 0.0
    total_weight = 0.0
    for a, b in zip(slots_a, slots_b):
        weight = top_face_color_pair_weight(a, b)
        accumulated += color_similarity(a, b) * weight
        total_weight += weight
    return 0.0 if total_weight == 0.0 else accumulated / total_weight


def top_face_color_pair_weight(a: Color, b: Color) -> float:
    return SHARED_WHITE_TOP_FACE_WEIGHT if is_whiteish_top_face_color(a) and is_whiteish_top_face_color(b) else 1.0


def is_whiteish_top_face_color(color: Color) -> bool:
    max_channel = max(color[0], color[1], color[2])
    min_channel = min(color[0], color[1], color[2])
    chroma = max_channel - min_channel
    return luminance(color) >= WHITEISH_LUMINANCE_THRESHOLD and chroma <= WHITEISH_CHROMA_THRESHOLD


def stripe_slots(style: Style) -> list[Color]:
    return [
        style.left_stripe_color,
        style.left_mid_stripe_color,
        style.center_stripe_color,
        style.right_mid_stripe_color,
        style.right_stripe_color,
    ]


def average_face_color(style: Style, size: int) -> Color:
    r = g = b = a = 0.0
    count = 0
    for i in range(size * size):
        if not is_inner_face_pixel(i, size):
            continue
        x = i % size
        y = i // size
        pixel = decal_pixel(style, x, y, size)
        if pixel[3] <= 0.0:
            continue
        r += pixel[0]
        g += pixel[1]
        b += pixel[2]
        a += pixel[3]
        count += 1
    return style.body_color if count == 0 else (r / count, g / count, b / count, a / count)


def top_face_palette(style: Style) -> tuple[Color, Color]:
    buckets: dict[int, tuple[int, float, float, float]] = {}
    size = FACE_DOMINANT_SAMPLE_TEXTURE_SIZE
    for i in range(size * size):
        if not is_inner_face_pixel(i, FACE_AVERAGE_SAMPLE_TEXTURE_SIZE):
            continue
        x = i % size
        y = i // size
        pixel = decal_pixel(style, x, y, size)
        if pixel[3] <= 0.0:
            continue
        key = quantize_rgb(pixel)
        count, r, g, b = buckets.get(key, (0, 0.0, 0.0, 0.0))
        buckets[key] = (count + 1, r + pixel[0], g + pixel[1], b + pixel[2])

    dominant = sorted(
        ((count, (r / count, g / count, b / count, 1.0)) for count, r, g, b in buckets.values()),
        key=lambda item: item[0],
        reverse=True,
    )
    if not dominant:
        return style.body_color, style.accent_color

    primary = dominant[0][1]
    secondary = style.accent_color
    for _, candidate in dominant:
        if not colors_equivalent(candidate, primary):
            secondary = candidate
            break
    else:
        secondary = first_distinct_configured_color(style)
    return primary, secondary


def swap_palette_color(pixel: Color, palette: tuple[Color, Color]) -> Color:
    primary, secondary = palette
    primary_distance = color_distance(pixel, primary)
    secondary_distance = color_distance(pixel, secondary)
    return secondary if primary_distance <= secondary_distance else primary


def decal_pixel(style: Style, x: int, y: int, size: int) -> Color:
    u = ((x + 0.5) / size * 2.0) - 1.0
    v = ((y + 0.5) / size * 2.0) - 1.0
    distance = math.sqrt((u * u) + (v * v))
    if distance > 0.98:
        return CLEAR

    if style.family == "Plain":
        return plain_pixel(style, distance)
    if style.family == "VerticalStripes":
        return vertical_stripes_pixel(style, u, v, distance)
    if style.family == "HorizontalStripes":
        return vertical_stripes_pixel(style, v, -u, distance)
    if style.family == "Sleeved":
        return sleeved_pixel(style, u, v, distance)
    if style.family == "SingleStriped":
        return single_striped_pixel(style, u, v, distance)
    if style.family == "Roma":
        return roma_pixel(style, u, v, distance)
    return style.body_color


def plain_pixel(style: Style, distance: float) -> Color:
    if distance > FACE_CIRCLE_RADIUS:
        return style.body_color
    if distance > FACE_INNER_RADIUS:
        return style.ring_color
    return style.accent_color


def vertical_stripes_pixel(style: Style, u: float, v: float, distance: float) -> Color:
    if distance > FACE_CIRCLE_RADIUS:
        return style.body_color
    if distance > FACE_INNER_RADIUS:
        return style.ring_color
    if should_reveal_background_through_center_stripe(style, u, v):
        return style.left_mid_stripe_color
    return vertical_stripe_band_color(style, u)


def sleeved_pixel(style: Style, u: float, v: float, distance: float) -> Color:
    if distance > FACE_CIRCLE_RADIUS:
        return style.body_color
    if v >= SLEEVE_MIN_V and abs(u) >= SLEEVE_DIAGONAL_BASE_X - (SLEEVE_DIAGONAL_SLOPE * v):
        return style.ring_color
    return style.body_color


def single_striped_pixel(style: Style, u: float, v: float, distance: float) -> Color:
    if distance > FACE_CIRCLE_RADIUS:
        return style.body_color
    stripe_half_width = normalized_stripe_half_width(style.stripe_width)
    radians = math.radians(style.stripe_direction_degrees)
    stripe_direction = (math.cos(radians), -math.sin(radians))
    stripe_normal = (-stripe_direction[1], stripe_direction[0])
    signed_distance = (u * stripe_normal[0]) + (v * stripe_normal[1])
    return style.accent_color if abs(signed_distance) <= stripe_half_width else style.body_color


def roma_pixel(style: Style, u: float, v: float, distance: float) -> Color:
    if distance > FACE_CIRCLE_RADIUS:
        return style.body_color
    if ROMA_TOP_STRIPE_MIN_V <= v <= ROMA_TOP_STRIPE_MAX_V:
        return style.accent_color
    if ROMA_BOTTOM_STRIPE_MIN_V <= v <= ROMA_BOTTOM_STRIPE_MAX_V:
        return style.ring_color
    return style.body_color


def vertical_stripe_band_color(style: Style, u: float) -> Color:
    stripe_width = (FACE_INNER_RADIUS * 2.0) / 5.0
    normalized_u = clamp(u, -FACE_INNER_RADIUS, FACE_INNER_RADIUS)
    band_start = -FACE_INNER_RADIUS
    if normalized_u < band_start + stripe_width:
        return style.left_stripe_color
    if normalized_u < band_start + (stripe_width * 2.0):
        return style.left_mid_stripe_color
    if normalized_u < band_start + (stripe_width * 3.0):
        return style.center_stripe_color
    if normalized_u < band_start + (stripe_width * 4.0):
        return style.right_mid_stripe_color
    return style.right_stripe_color


def should_reveal_background_through_center_stripe(style: Style, u: float, v: float) -> bool:
    if style.center_stripe_mode != "BreakUnderNumber":
        return False
    stripe_width = (FACE_INNER_RADIUS * 2.0) / 5.0
    center_left = -FACE_INNER_RADIUS + (stripe_width * 2.0)
    center_right = center_left + stripe_width
    return center_left <= u <= center_right and VERTICAL_CENTER_STRIPE_GAP_BOTTOM <= v <= VERTICAL_CENTER_STRIPE_GAP_TOP


def color_similarity(a: Color, b: Color) -> float:
    closeness = 1.0 - clamp(color_distance(a, b), 0.0, 1.0)
    return closeness * closeness * 100.0


def color_distance(a: Color, b: Color) -> float:
    return math.sqrt(
        (0.2126 * ((a[0] - b[0]) ** 2))
        + (0.7152 * ((a[1] - b[1]) ** 2))
        + (0.0722 * ((a[2] - b[2]) ** 2))
    )


def is_inner_face_pixel(pixel_index: int, texture_size: int) -> bool:
    x = pixel_index % texture_size
    y = pixel_index // texture_size
    u = ((x + 0.5) / texture_size * 2.0) - 1.0
    v = ((y + 0.5) / texture_size * 2.0) - 1.0
    return math.sqrt((u * u) + (v * v)) <= FACE_INNER_RADIUS_FOR_SIMILARITY


def normalized_stripe_half_width(raw_width: float) -> float:
    normalized_width = raw_width if raw_width <= 1.0 else raw_width / 100.0
    return clamp(normalized_width, 0.0, 1.0) * FACE_CIRCLE_RADIUS


def first_distinct_configured_color(style: Style) -> Color:
    for candidate in (
        style.accent_color,
        style.ring_color,
        style.left_stripe_color,
        style.left_mid_stripe_color,
        style.center_stripe_color,
        style.right_mid_stripe_color,
        style.right_stripe_color,
        style.number_color,
    ):
        if candidate[3] > 0.0 and not colors_equivalent(candidate, style.body_color):
            return candidate
    return readable_contrast_color(style.body_color)


def readable_contrast_color(color: Color) -> Color:
    return BLACK if luminance(color) >= 0.5 else WHITE


def luminance(color: Color) -> float:
    return (0.2126 * color[0]) + (0.7152 * color[1]) + (0.0722 * color[2])


def colors_equivalent(a: Color, b: Color) -> bool:
    return abs(a[0] - b[0]) <= SAME_COLOR_TOLERANCE and abs(a[1] - b[1]) <= SAME_COLOR_TOLERANCE and abs(a[2] - b[2]) <= SAME_COLOR_TOLERANCE


def quantize_rgb(color: Color) -> int:
    r = round(clamp(color[0], 0.0, 1.0) * 255.0)
    g = round(clamp(color[1], 0.0, 1.0) * 255.0)
    b = round(clamp(color[2], 0.0, 1.0) * 255.0)
    return (r << 16) | (g << 8) | b


def looks_like_single_striped(config: dict[str, Any]) -> bool:
    return any(key in config for key in ("stripeColor", "stripeWidth", "stripeRotation", "stripeDirection"))


def parse_color(raw: Any, fallback: Color) -> Color:
    if not raw or not isinstance(raw, str):
        return fallback
    normalized = raw.strip()
    while normalized.startswith("##"):
        normalized = normalized[1:]
    if not normalized.startswith("#"):
        normalized = "#" + normalized
    hex_value = normalized[1:]
    if len(hex_value) == 6:
        hex_value += "FF"
    if len(hex_value) != 8:
        return fallback
    try:
        return (
            int(hex_value[0:2], 16) / 255.0,
            int(hex_value[2:4], 16) / 255.0,
            int(hex_value[4:6], 16) / 255.0,
            int(hex_value[6:8], 16) / 255.0,
        )
    except ValueError:
        return fallback


def parse_center_mode(raw: Any) -> str:
    value = str(raw or "").strip()
    return value if value in {"None", "Solid", "BreakUnderNumber"} else "None"


def parse_stripe_width(raw: Any) -> float:
    if raw is None:
        return 0.25
    try:
        return max(0.0, float(raw))
    except (TypeError, ValueError):
        return 0.25


def parse_stripe_direction(direction: Any, rotation: Any) -> float:
    source = direction if direction is not None else rotation
    if source is None:
        return 0.0
    try:
        parsed = float(source)
    except (TypeError, ValueError):
        return 0.0
    parsed %= 180.0
    return parsed + 180.0 if parsed < 0.0 else parsed


def primary_label(raw: dict[str, Any]) -> str:
    value = raw.get("example", raw.get("examples", ""))
    if isinstance(value, str):
        return value.strip()
    if isinstance(value, list):
        for item in value:
            if isinstance(item, str) and item.strip():
                return item.strip()
    return ""


def short_label(kit: Kit) -> str:
    return f"{kit.kit_id}<br>{kit.display_name}"


def ratio(score: float) -> str:
    value = score / 100.0
    formatted = f"{value:.3f}"
    return formatted[1:] if formatted.startswith("0.") else formatted


def escape_cell(value: str) -> str:
    return value.replace("|", "\\|").replace("\n", " ")


def clamp(value: float, minimum: float, maximum: float) -> float:
    return max(minimum, min(maximum, value))


CLEAR: Color = (0.0, 0.0, 0.0, 0.0)
BLACK: Color = (0.0, 0.0, 0.0, 1.0)
WHITE: Color = (1.0, 1.0, 1.0, 1.0)


if __name__ == "__main__":
    main()

#!/usr/bin/env bash
# Builds Assets/*.wav from two real cow recordings (Wikimedia Commons, see README_sounds.md).
# Usage: tools/make_real_sounds.sh <Single_Cow_Moo.ogg> <Mudchute_cow_1.ogg>
# Calf and bull are the same recordings with a higher or lower pitch (pitch and speed change together).
set -euo pipefail
S="$1"; M="$2"; OUT="$(dirname "$0")/../Assets"
# cut <src> <start> <dur> <pitch> <out>: trim, change pitch, fade the ends, mono 22.05 kHz 16 bit
cut() {
  local src="$1" start="$2" dur="$3" k="$4" out="$5"
  local rate; rate=$(python3 -c "print(int(44100*$k))")
  local d2;   d2=$(python3 -c "print(round($dur/$k,3))")
  local fo;   fo=$(python3 -c "print(round(min(0.25,$dur/$k/3),3))")
  ffmpeg -v error -y -ss "$start" -t "$dur" -i "$src" -af \
    "aresample=44100,asetrate=$rate,aresample=22050,afade=t=in:d=0.01,afade=t=out:st=$(python3 -c "print(max(0,round($d2-$fo,3)))"):d=$fo,loudnorm=I=-16:TP=-1.5" \
    -ac 1 -ar 22050 -c:a pcm_s16le "$OUT/$out.wav"
}
# cow
cut "$S" 0.10 2.7 1.0  cow_idle1
cut "$M" 0.00 2.2 1.0  cow_idle2
cut "$M" 0.00 1.1 1.05 cow_alert
cut "$M" 0.00 0.55 1.05 cow_hit1
cut "$S" 0.10 0.65 1.08 cow_hit2
cut "$S" 0.10 2.7 0.80 cow_death
# calf
cut "$M" 0.00 2.2 1.40 calf_idle1
cut "$S" 0.10 1.6 1.35 calf_idle2
cut "$M" 0.00 0.5 1.50 calf_hit
cut "$S" 0.10 2.7 1.30 calf_death
# Momo
cut "$S" 0.10 2.7 0.72 bull_idle1
cut "$M" 0.00 2.2 0.70 bull_idle2
cut "$M" 0.00 1.1 0.76 bull_alert
cut "$S" 0.10 0.6 0.78 bull_hit
cut "$S" 0.10 2.7 0.60 bull_death

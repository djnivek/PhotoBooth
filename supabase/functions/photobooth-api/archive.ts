// Deterministic ZIP with stored JPEG entries. Assembly happens beside Storage,
// avoiding a second upload of every photograph over the booth's mobile uplink.
export function buildArchive(entries: { name: string; bytes: Uint8Array }[]): Uint8Array {
  const names = entries.map(e => new TextEncoder().encode(e.name));
  const total = entries.reduce((sum, entry, i) => sum + entry.bytes.length + 76 + names[i].length * 2, 22);
  if (total > 50 * 1024 * 1024) throw new Error("archive_too_large");
  const output = new Uint8Array(total); const view = new DataView(output.buffer);
  const central: { offset: number; crc: number }[] = []; let at = 0;
  const date = ((2026 - 1980) << 9) | (1 << 5) | 1;
  entries.forEach((entry, i) => {
    const crc = crc32(entry.bytes); central.push({ offset: at, crc });
    view.setUint32(at, 0x04034b50, true); view.setUint16(at + 4, 20, true);
    view.setUint16(at + 12, date, true); view.setUint32(at + 14, crc, true);
    view.setUint32(at + 18, entry.bytes.length, true); view.setUint32(at + 22, entry.bytes.length, true);
    view.setUint16(at + 26, names[i].length, true);
    output.set(names[i], at + 30); output.set(entry.bytes, at + 30 + names[i].length);
    at += 30 + names[i].length + entry.bytes.length;
  });
  const directory = at;
  entries.forEach((entry, i) => {
    view.setUint32(at, 0x02014b50, true); view.setUint16(at + 4, 20, true); view.setUint16(at + 6, 20, true);
    view.setUint16(at + 14, date, true); view.setUint32(at + 16, central[i].crc, true);
    view.setUint32(at + 20, entry.bytes.length, true); view.setUint32(at + 24, entry.bytes.length, true);
    view.setUint16(at + 28, names[i].length, true); view.setUint32(at + 42, central[i].offset, true);
    output.set(names[i], at + 46); at += 46 + names[i].length;
  });
  view.setUint32(at, 0x06054b50, true); view.setUint16(at + 8, entries.length, true); view.setUint16(at + 10, entries.length, true);
  view.setUint32(at + 12, at - directory, true); view.setUint32(at + 16, directory, true);
  return output;
}
const crcTable = Uint32Array.from({ length: 256 }, (_, n) => {
  let value = n; for (let bit = 0; bit < 8; bit++) value = value & 1 ? 0xedb88320 ^ (value >>> 1) : value >>> 1;
  return value >>> 0;
});
function crc32(bytes: Uint8Array): number {
  let crc = 0xffffffff; for (const byte of bytes) crc = crcTable[(crc ^ byte) & 255] ^ (crc >>> 8);
  return (crc ^ 0xffffffff) >>> 0;
}

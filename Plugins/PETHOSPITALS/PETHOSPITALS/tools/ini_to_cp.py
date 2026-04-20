import struct
import sys

def write_str(f, s):
    # use cp1252 to preserve accented characters used in the INI files
    b = s.encode("cp1252", errors="replace")
    f.write(struct.pack("<I", len(b)))
    f.write(b)

def compile_ini(infile, outfile):
    section = None
    entries = []

    # Read source INI using CP1252 to match the file encoding
    with open(infile, encoding="cp1252", errors="replace") as f:
        for line in f:
            line = line.strip()

            if not line or line.startswith(";"):
                continue

            if line.startswith("[") and line.endswith("]"):
                section = line[1:-1]
                continue

            if "=" in line:
                k, v = line.split("=", 1)
                entries.append((k.strip(), v.strip()))

    with open(outfile, "wb") as f:
        # section count
        f.write(struct.pack("<I", 1))

        # section name
        write_str(f, section)

        # entry count
        f.write(struct.pack("<I", len(entries)))

        for k, v in entries:
            write_str(f, k)
            write_str(f, v)

if __name__ == "__main__":
    infile = sys.argv[1]
    outfile = infile + ".cp"
    compile_ini(infile, outfile)

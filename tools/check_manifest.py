import re, sys
path = sys.argv[1]
data = open(path, 'rb').read()
found = False
for m in re.finditer(rb'requestedExecutionLevel[^>]*level="[^"]*"', data):
    print('EXE嵌入:', m.group(0).decode('utf-8', 'ignore'))
    found = True
if not found:
    print('未找到 requestedExecutionLevel')

import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
import sys

if len(sys.argv) == 2:
    inp = sys.argv[1]
else:
    inp = "export.json"

if len(sys.argv) == 3:
    fileName = sys.argv[2]
else:
    fileName = "fig.png"

data = pd.read_json(inp)

x, y, z = np.indices((10, 10, 10))
m = (z < 1)
cubes = []
for k in range(4):
    cubes.append((x < 0) & (y < 0) & (z < 0))
    for i in range(10):
        for j in range(10):
            if data.iloc[i,j] == k + 1:
                cubes[k] = cubes[k] | (x >= i) & (x < i + 1) & (y >= j) & (y < j + 1) & (z >= 1) & (z < 2)

voxels = m | cubes[0] | cubes[1] | cubes[2] | cubes[3]
colors = np.empty(voxels.shape, dtype=object)
colors[m] = 'white'
colors[cubes[0]] = 'red'
colors[cubes[1]] = 'green'
colors[cubes[2]] = 'yellow'
colors[cubes[3]] = 'blue'

plt.tight_layout()
ax = plt.figure().add_subplot(projection='3d')
ax.set_xticks([i for i in range(0, 11)])
ax.invert_xaxis()
ax.set_yticks([i for i in range(0, 11)])
ax.set_zticks([])
ax.xaxis.set_pane_color((1.0, 1.0, 1.0, 0.0))
ax.yaxis.set_pane_color((1.0, 1.0, 1.0, 0.0))
ax.xaxis._axinfo["grid"]['color'] =  (1,1,1,0)
ax.yaxis._axinfo["grid"]['color'] =  (1,1,1,0)
ax.zaxis._axinfo["grid"]['color'] =  (1,1,1,0)
ax.tick_params(axis='x', colors='red')
ax.tick_params(axis='y', colors='red')
ax.set_xticklabels(ax.get_xticks(), 
                verticalalignment='baseline',
                horizontalalignment='left')
ax.set_yticklabels(ax.get_yticks(), 
                verticalalignment='baseline',
                horizontalalignment='left')


ax.voxels(voxels, facecolors=colors, edgecolor='k')

plt.savefig(fileName, transparent=True)
print(fileName)
import numpy as np
import math
def conf_inter(list):
    N=len(nlist)
    narray = np.array(nlist)
#加权均值计算
    nsum_1 = sum(narray)
    wlist = [float(i)/nsum_1 for i in nlist]
    wmean = sum(narray*wlist)
    print("Weighted mean: ", wmean)
#方差计算
    s = sum((i-wmean)**2 for i in nlist)
    std = math.sqrt(s/N)
#方差输出
    print("Standard deviation: ", std)
    print ("Confidence interval: [%f, %f]"%(wmean-std,wmean+std))
    return wmean-std, wmean+std

if __name__ == "__main__":
    nlist=[8,1,9,50]
    min_count, max_count = conf_inter(nlist)
    print min_count
    i = 0
    for j in nlist:
        if j >min_count and j<max_count:
            nlist[i]=1
        else:
            nlist[i]=0
        i = i+1
    print(nlist)
            

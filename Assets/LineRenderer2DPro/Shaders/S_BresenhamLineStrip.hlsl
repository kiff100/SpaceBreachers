// Copyright 2024 Alejandro Villalba Avila

#if MAXIMUM_AMOUNT_OF_POINTS__UNLIMITED
	#define PACKED_POINTS_PARAM Texture2D tPackedPoints
#else
	#define PACKED_POINTS_PARAM float4 arPackedPoints[MAXIMUM_AMOUNT_OF_POINTS]
#endif

void IsPixelInLine(int nCurrentLineIndex, float fThickness, float2 vPointP, float2 vOrigin, PACKED_POINTS_PARAM, out bool bOutIsPixelInLine, out float fOutPixelIndex)
{
	float2 vThickness = float2(fThickness, fThickness);

	// Origin in screen space, in pixels
	vOrigin *= _ScreenParams.xy;
	
	// The amount of pixels the camera has moved regarding a thickness-wide block of pixels
	vOrigin = fmod(vOrigin, vThickness);
	vOrigin = round(vOrigin);
	
	// This moves the line N pixels, this is necessary due to the camera moves 1 pixel each time and the line may be wider than 1 pixel
	// so this avoids the line jumping from one block (thickness-wide) to the next, and instead its movement is smoother by moving pixel by pixel
	vPointP += vThickness - vOrigin;
	
	vPointP = vPointP - fmod(vPointP, vThickness);
	vPointP = round(vPointP / vThickness) ;

	bOutIsPixelInLine = false;
	float nPixelIndex = -1.0f;
		
	for(int t = 0; t < nCurrentLineIndex + 1; ++t) // Performance improvement: Only until current segment
	{
		int nXCoord = floor(t / 2.0f);

#if MAXIMUM_AMOUNT_OF_POINTS__UNLIMITED
		float4 vPackedPoints = tPackedPoints.Load(int3(nXCoord, 0, 0));
		float4 vPackedPoints2 = tPackedPoints.Load(int3(nXCoord + 1, 0, 0));
#else
		float4 vPackedPoints = arPackedPoints[nXCoord];
		float4 vPackedPoints2 = arPackedPoints[nXCoord + 1];
#endif

		float2 worldSpaceEndpointA = fmod(t, 2) == 0 ? vPackedPoints.rg : vPackedPoints.ba;
		float2 worldSpaceEndpointB = fmod(t, 2) == 0 ? vPackedPoints.ba : vPackedPoints2.rg;

		float4 projectionSpaceEndpointA = mul(UNITY_MATRIX_VP, float4(worldSpaceEndpointA.x, worldSpaceEndpointA.y, 0.0f, 1.0f));
		float4 projectionSpaceEndpointB = mul(UNITY_MATRIX_VP, float4(worldSpaceEndpointB.x, worldSpaceEndpointB.y, 0.0f, 1.0f));
		
		// Endpoints in screen space
		float2 vEndpointA = ComputeScreenPos(projectionSpaceEndpointA).xy * _ScreenParams.xy;
		float2 vEndpointB = ComputeScreenPos(projectionSpaceEndpointB).xy * _ScreenParams.xy;
		
		vEndpointA = round(vEndpointA);
		vEndpointB = round(vEndpointB);
	
		vEndpointA += vThickness - vOrigin;
		vEndpointB += vThickness - vOrigin;
		
		vEndpointA = vEndpointA - fmod(vEndpointA, vThickness);
		vEndpointB = vEndpointB - fmod(vEndpointB, vThickness);
		vEndpointA = round(vEndpointA / vThickness) ;
		vEndpointB = round(vEndpointB / vThickness) ;
		 
		int x = vEndpointA.x;
		int y = vEndpointA.y;
		int x2 = vEndpointB.x;
		int y2 = vEndpointB.y;
		int pX = vPointP.x;
		int pY = vPointP.y;
		int w = x2 - x;
		int h = y2 - y;
		int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;

		if (w<0) dx1 = -1 ; else if (w>0) dx1 = 1;
		if (h<0) dy1 = -1 ; else if (h>0) dy1 = 1;
		if (w<0) dx2 = -1 ; else if (w>0) dx2 = 1;

		int nLongest = abs(w);
		int nShortest = abs(h);

		if (nLongest <= nShortest)
		{
			nLongest = abs(h);
			nShortest = abs(w);

			if (h < 0)
				dy2 = -1; 
			else if (h > 0)
				dy2 = 1;
			
			dx2 = 0;
		}
		
		if (t < nCurrentLineIndex)
		{
			// Preformance improvement: It skips the entire segment because it is previous to the segment that is being rendered
			nPixelIndex += nLongest;
			continue;
		}
		
		int nNumerator = nLongest >> 1;

		float2 lineDirection = vEndpointB - vEndpointA;

		for (int i = 0; i <= nLongest; ++i)
		{
			nPixelIndex++;

			if(x == pX && y == pY)
			{
				// Current pixel coinides with line pixel, we can stop searching and return the results
				bOutIsPixelInLine = t < 1 || i > 0; // The first pixel of each segment, from the 2nd onwards, is discarded to avoid redrawing
				fOutPixelIndex = nPixelIndex;
				return;
			}
			
			if (dot(vPointP - float2(x, y), lineDirection) < 0.0f)
			{
				// Performance improvement: It skips the segment if the current pixel is posterior to segment pixel
				nPixelIndex += (nLongest - i);
				break;
			}
			
			nNumerator += nShortest;

			if (nNumerator >= nLongest)
			{
				nNumerator -= nLongest;
				x += dx1;
				y += dy1;
			}
			else
			{
				x += dx2;
				y += dy2;
			}
		}

		--nPixelIndex; // This is necessary because contigous line endpoints occupy the same position, the index of the next pixels of the line after the endpoint must be offset so the overlapped pixel is counted
	}
}

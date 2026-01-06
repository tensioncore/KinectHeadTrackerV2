#pragma once

HRESULT VisualizeFacetracker(IFTImage* pColorImg, IFTResult* pAAMRlt, UINT32 color);

HRESULT VisualizeFaceModel(IFTImage* pColorImg, IFTModel* pModel, FT_CAMERA_CONFIG const* pCameraConfig, FLOAT const* pSUCoef, 
    FLOAT zoomFactor, POINT viewOffset, IFTResult* pAAMRlt, UINT32 color);



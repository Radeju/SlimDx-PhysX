float4x4 mWIT : WorldInverseTranspose;
float4x4 mWVP : WorldViewProjection;
float4x4 mW : World;
float4x4 mVI: ViewInverse;


float3 xLightPos  = {-25.0f,100.0f,75.0f};
float3 xLightColor = {1.0f,1.0f,1.0f};

// Ambient Light
float3 xAmbientColor = {0.07f,0.07f,0.07f};

float xKs  = 0.1;  // specular intensity

float xEccentricity = 0.3; //Highlight Eccentricity


float xBump  = 0.0; // Bump intensity

float xKr  = 0.01; // Reflection intensity

float xMass;
float xMaxMass;

texture2D xDiffuseTexture; 


SamplerState PlanarSampler
{
	Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
       
};

texture2D xBumpTexture;


texture2D xReflectionTexture;

SamplerState CubeSampler 
{    
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Clamp;
    AddressV = Clamp;
    AddressW = Clamp;
};



struct VS_IN {
    float3 Position	: POSITION;
    float4 UV		: TEXCOORD0;
    float4 Normal	: NORMAL;
    float4 Tangent	: TANGENT;
    float4 Binormal	: BINORMAL;
};


struct PS_IN {
    float4 HPosition	: SV_POSITION;
    float2 UV		: TEXCOORD0;
    float3 LightVec	: TEXCOORD1;
    float3 WorldNormal	: TEXCOORD2;
    float3 WorldTangent	: TEXCOORD3;
    float3 WorldBinormal : TEXCOORD4;
    float3 WorldView	: TEXCOORD5;
};
 

 struct VS2_IN {
    float3 Position	: POSITION;
    float4 Normal	: NORMAL;
	float4 UV		: TEXCOORD0;
};

struct PS2_IN {
    float4 HPosition	: SV_POSITION;
    float2 UV		: TEXCOORD0;
    float3 LightVec	: TEXCOORD1;
    float3 WorldNormal	: TEXCOORD2;
};



PS_IN VS(VS_IN IN) {
    PS_IN OUT = (PS_IN)0;
    OUT.WorldNormal = mul(IN.Normal,mWIT).xyz;
    OUT.WorldTangent = mul(IN.Tangent,mWIT).xyz;
    OUT.WorldBinormal = mul(IN.Binormal,mWIT).xyz;
    float4 Po = float4(IN.Position.xyz,1);
    float3 Pw = mul(Po,mW).xyz;
    OUT.LightVec = (xLightPos - Pw);

    OUT.UV = float2(1- IN.UV.x, 1- IN.UV.y);
    OUT.WorldView = normalize(mVI[3].xyz - Pw);
    OUT.HPosition = mul(Po,mWVP);
    return OUT;
}

PS_IN VS2(VS2_IN IN) {
    PS_IN OUT = (PS_IN)0;
    OUT.WorldNormal = mul(IN.Normal,mWIT).xyz;
    float4 Po = float4(IN.Position.xyz,1);
    float3 Pw = mul(Po,mW).xyz;
    OUT.LightVec = (xLightPos - Pw);

    OUT.UV = float2(1- IN.UV.x, 1- IN.UV.y);
    OUT.HPosition = mul(Po,mWVP);
    return OUT;
}



void blinn_shading(PS_IN IN,
		    float3 LightColor,
		    float3 Nn,
		    float3 Ln,
		    float3 Vn,
		    out float3 DiffuseContrib,
		    out float3 SpecularContrib)
{
    float3 Hn = normalize(Vn + Ln);
    float hdn = dot(Hn,Nn);
    float3 R = reflect(-Ln,Nn);
    float rdv = dot(R,Vn);
    rdv = max(rdv,0.001);
    float ldn=dot(Ln,Nn);
    ldn = max(ldn,0.0);
    float ndv = dot(Nn,Vn);
    float hdv = dot(Hn,Vn);
    float eSq = xEccentricity*xEccentricity;
    float distrib = eSq / (rdv * rdv * (eSq - 1.0) + 1.0);
    distrib = distrib * distrib;
    float Gb = 2.0 * hdn * ndv / hdv;
    float Gc = 2.0 * hdn * ldn / hdv;
    float Ga = min(1.0,min(Gb,Gc));
    float fresnelHack = 1.0 - pow(ndv,5.0);
    hdn = distrib * Ga * fresnelHack / ndv;
    DiffuseContrib = ldn * LightColor;
    SpecularContrib = hdn * xKs * LightColor;
}

float4 PS(PS_IN IN) :SV_TARGET {
    float3 diffContrib;
    float3 specContrib;
    float3 Ln = normalize(IN.LightVec);
    float3 Vn = normalize(IN.WorldView);
    float3 Nn = normalize(IN.WorldNormal);
    float3 Tn = normalize(IN.WorldTangent);
    float3 Bn = normalize(IN.WorldBinormal);
    float3 bump = xBump * (xBumpTexture.Sample(PlanarSampler,IN.UV).rgb - float3(0.5,0.5,0.5));
    Nn = Nn + bump.x*Tn + bump.y*Bn;
    Nn = normalize(Nn);
	blinn_shading(IN,xLightColor,Nn,Ln,Vn,diffContrib,specContrib);
    float3 diffuseColor = xDiffuseTexture.Sample(PlanarSampler, float2(1-IN.UV.x,IN.UV.y)).rgb;
    float3 result = specContrib+(diffuseColor*(diffContrib+xAmbientColor));
    float3 R = -reflect(Vn,Nn);
    float3 reflColor = xKr * xReflectionTexture.Sample(CubeSampler,R.xyz).rgb;
    result += diffuseColor*reflColor;
    return float4(result,1);
}

float4 PS2(PS2_IN IN) :SV_TARGET {
    float3 diffContrib;
    float3 specContrib;
    float3 Ln = normalize(IN.LightVec);
    float3 Nn = normalize(IN.WorldNormal);
    float3 diffuseColor = xDiffuseTexture.Sample(PlanarSampler, float2(1-IN.UV.x,IN.UV.y)).rgb;    

	float3 massColor = diffuseColor * (xMaxMass - xMass) / xMaxMass;
	//float3 massColor = float3(xMass / xMaxMass, xMass / xMaxMass, xMass / xMaxMass);
	return float4(massColor,1);//*dot(Ln,Nn);

    //return float4(diffuseColor,1)*dot(Ln, Nn);
}



struct Simple_VS_IN {
    float3 Position	: POSITION;
};


struct Simple_PS_IN {
    float4 HPosition	: SV_POSITION;
};


Simple_PS_IN Simple_VS(Simple_VS_IN IN) {
	Simple_PS_IN OUT;
	float4 Po = float4(IN.Position.xyz,1);
	OUT.HPosition = mul(Po,mWVP);
	return OUT;
}

float4 Simple_PS(Simple_PS_IN IN) :SV_TARGET {
	return float4(0,0,0,0);
}


RasterizerState DisableCulling
{
    CullMode = NONE;
};


DepthStencilState DisableDepth
{
    DepthEnable = FALSE;
    DepthWriteMask = ZERO;
};

technique10 Blinn 	
{
    pass p0 {
        SetVertexShader( CompileShader( vs_4_0, VS() ) );
        SetGeometryShader( NULL );
        SetPixelShader( CompileShader( ps_4_0, PS() ) );
                
    
		
    }
}

technique10 SimpleLight
{
    pass p0 {
        SetVertexShader( CompileShader( vs_4_0, VS2() ) );
        SetGeometryShader( NULL );
        SetPixelShader( CompileShader( ps_4_0, PS2() ) );
		 SetRasterizerState(DisableCulling);               		
    }
	
}

technique10 Simple 	
{
    pass p0 {
        SetVertexShader( CompileShader( vs_4_0, Simple_VS() ) );
        SetGeometryShader( NULL );
        SetPixelShader( CompileShader( ps_4_0, Simple_PS() ) );
                		  
		
    }
}

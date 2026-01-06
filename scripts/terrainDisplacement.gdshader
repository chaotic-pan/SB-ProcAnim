shader_type spatial;
render_mode unshaded;

void vertex() {
    VERTEX.y += cos(VERTEX.x*4.0) * sin(VERTEX.z*4.0) *0.5;
}

void fragment() {
	// Called for every pixel the material is visible on.
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}

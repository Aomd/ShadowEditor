/**
 * 圆柱体
 * @param {THREE.CylinderBufferGeometry} geometry 几何体
 * @param {THREE.MeshStandardMaterial} material 材质
 */
function Cylinder(geometry = new THREE.CylinderBufferGeometry(1, 1, 2, 32, 1, false), material = new THREE.MeshStandardMaterial()) {
    THREE.Mesh.call(this, geometry, material);

    this.name = _t('Cylinder');
    this.castShadow = true;
    this.receiveShadow = true;

    this.userData.physics = this.userData.physics || {
        enabled: true,
        type: 'rigidBody',
        shape: 'btBoxShape',
        mass: 1,
        inertia: {
            x: 0,
            y: 0,
            z: 0
        }
    };
}

Cylinder.prototype = Object.create(THREE.Mesh.prototype);
Cylinder.prototype.constructor = Cylinder;

export default Cylinder;
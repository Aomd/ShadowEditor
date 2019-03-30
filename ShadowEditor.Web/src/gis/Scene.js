import Globe from './Globe';

/**
 * GIS场景
 * @param {*} app 
 */
function Scene(app) {
    this.app = app;
}

Scene.prototype.start = function () {
    var editor = this.app.editor;

    this.oldBackground = editor.scene.background;
    editor.scene.background = new THREE.CubeTextureLoader().load([
        'assets/textures/MilkyWay/dark-s_px.jpg',
        'assets/textures/MilkyWay/dark-s_nx.jpg',
        'assets/textures/MilkyWay/dark-s_py.jpg',
        'assets/textures/MilkyWay/dark-s_ny.jpg',
        'assets/textures/MilkyWay/dark-s_pz.jpg',
        'assets/textures/MilkyWay/dark-s_nz.jpg',
    ]);
    editor.sceneHelpers.visible = false

    editor.controls.enabled = false;
    editor.controls.dispose();
    editor.transformControls.enabled = false;

    this.app.editor.showViewHelper = false;

    this.globe = new Globe(editor.camera, editor.renderer);
    editor.scene.add(this.globe);

    this.app.on(`afterRender.${this.id}`, this.update.bind(this));
};

Scene.prototype.update = function () {
    this.globe.update();
};

Scene.prototype.stop = function () {
    this.app.on(`afterRender.${this.id}`, null);
    this.globe.dispose();
    delete this.globe;

    var editor = this.app.editor;

    editor.background = this.oldBackground;
    delete this.oldBackground;
    editor.sceneHelpers.visible = true;

    editor.controls = new THREE.EditorControls(editor.camera, editor.renderer.domElement);
    editor.transformControls.enabled = true;

    this.app.editor.showViewHelper = true;
};

export default Scene;
describe('11111', function () {
    describe('11111aaa', function () {
        it('should have a mocha div', function () {
            $('#moch').should.not.exist;
            $('#mocha').should.exist;
            expect($('#moch')).not.to.exist;
            true.should.equal(false);
        })
    })
});

describe('22222', function () {
    describe('22222aaa', function () {
        it('should have at least two divs', function () {
            $('body > div').length.should.be.at.least(2);
        })
    })
});
